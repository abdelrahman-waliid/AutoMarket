import hashlib
import os
import random
import time
from datetime import datetime, timezone
from typing import Any, Dict, Optional, Tuple

import joblib
import numpy as np
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestRegressor
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder, StandardScaler

try:
    from xgboost import XGBRegressor
except Exception:  # pragma: no cover
    XGBRegressor = None

try:
    from scipy import sparse
except Exception:  # pragma: no cover
    sparse = None


RANDOM_STATE = 42
TEST_SIZE = 0.20
MIN_YEAR = 1990
MAX_MILEAGE = 300_000
MIN_PRICE = 50_000.0
MAX_PRICE = 12_000_000.0
MODEL_ARTIFACT_VERSION = "car-price-regressor-v2"

MODEL_MAE: Optional[float] = None
MODEL_RMSE: Optional[float] = None
MODEL_R2: Optional[float] = None
MODEL_VERSION: Optional[str] = None
MODEL_TYPE: Optional[str] = None
MODEL_TRAINED_AT: Optional[str] = None

NUMERIC_FEATURES: Tuple[str, ...] = (
    "year",
    "mileage",
    "car_age",
    "mileage_per_year",
    "condition_score",
)
CATEGORICAL_FEATURES: Tuple[str, ...] = (
    "brand",
    "fuelType",
    "transmission",
    "condition",
    "brand_tier",
)
MODEL_FEATURES: Tuple[str, ...] = NUMERIC_FEATURES + CATEGORICAL_FEATURES

REQUIRED_COLUMNS = {"year", "mileage", "fuelType", "transmission", "price"}
HASH_COLUMNS: Tuple[str, ...] = (
    "brand",
    "year",
    "mileage",
    "fuelType",
    "transmission",
    "condition",
    "price",
)

DEFAULT_VALUES = {
    "brand": "Unknown",
    "fuelType": "Petrol",
    "transmission": "Manual",
    "condition": "used",
}

BRAND_TIER_BY_KEY = {
    "bmw": "premium",
    "mercedes": "premium",
    "audi": "premium",
    "lexus": "premium",
    "volvo": "premium",
    "toyota": "mid",
    "honda": "mid",
    "skoda": "mid",
    "mazda": "mid",
    "nissan": "budget",
    "kia": "budget",
    "hyundai": "budget",
    "chevrolet": "budget",
    "mg": "budget",
    "byd": "budget",
}

CONDITION_SCORE_BY_VALUE = {
    "new": 1.00,
    "used": 0.72,
    "refurbished": 0.55,
}

FUEL_ALIASES = {
    "petrol": "Petrol",
    "gasoline": "Petrol",
    "diesel": "Diesel",
    "hybrid": "Hybrid",
    "pluginhybrid": "Hybrid",
    "electric": "Hybrid",
    "cng": "Petrol",
}

TRANSMISSION_ALIASES = {
    "manual": "Manual",
    "automatic": "Automatic",
    "cvt": "Automatic",
    "semiautomatic": "Automatic",
}

CONDITION_ALIASES = {
    "new": "new",
    "used": "used",
    "veryused": "refurbished",
    "refurbished": "refurbished",
    "excellent": "new",
    "good": "used",
    "fair": "refurbished",
    "poor": "refurbished",
}


def ensure_sample_dataset(csv_path: str) -> None:
    if os.path.exists(csv_path):
        return

    os.makedirs(os.path.dirname(csv_path), exist_ok=True)

    rng = random.Random(RANDOM_STATE)
    brands = ["Toyota", "Hyundai", "Kia", "Nissan", "BMW", "Mercedes"]
    fuel_types = ["Petrol", "Diesel", "Hybrid"]
    transmissions = ["Manual", "Automatic"]
    conditions = ["new", "used", "refurbished"]

    rows = []
    for _ in range(1000):
        brand = rng.choice(brands)
        year = rng.randint(1998, 2025)
        age = max(0, 2025 - year)
        mileage = max(0, int(rng.gauss(15_000 * max(age, 1), 15_000)))
        mileage = min(mileage, MAX_MILEAGE)
        fuel = rng.choice(fuel_types)
        transmission = rng.choice(transmissions)
        condition = rng.choices(conditions, weights=[0.10, 0.70, 0.20], k=1)[0]

        base_price = {
            "BMW": 2_700_000,
            "Mercedes": 3_300_000,
            "Toyota": 1_300_000,
            "Hyundai": 1_050_000,
            "Kia": 1_000_000,
            "Nissan": 950_000,
        }.get(brand, 900_000)

        year_factor = max(0.20, 0.94**age)
        mileage_factor = max(0.60, 1.0 - (mileage / MAX_MILEAGE) * 0.35)
        condition_factor = {
            "new": 1.08,
            "used": 1.00,
            "refurbished": 0.87,
        }[condition]
        transmission_factor = 1.03 if transmission == "Automatic" else 1.00
        fuel_factor = 1.06 if fuel == "Hybrid" else (1.02 if fuel == "Diesel" else 1.00)
        noise_factor = 1.0 + rng.uniform(-0.05, 0.05)

        price = (
            base_price
            * year_factor
            * mileage_factor
            * condition_factor
            * transmission_factor
            * fuel_factor
            * noise_factor
        )
        price = float(np.clip(price, MIN_PRICE, MAX_PRICE))

        rows.append(
            {
                "brand": brand,
                "year": year,
                "mileage": mileage,
                "fuelType": fuel,
                "transmission": transmission,
                "condition": condition,
                "price": round(price, 2),
            }
        )

    pd.DataFrame(rows).to_csv(csv_path, index=False)


def load_or_train_model(csv_path: str, model_path: str) -> Dict[str, Any]:
    raw_df = pd.read_csv(csv_path)
    training_df = _prepare_training_dataframe(raw_df)

    if training_df.empty:
        raise ValueError("No valid rows remain after data quality filtering.")

    dataset_hash = _compute_dataset_hash(training_df)

    if os.path.exists(model_path):
        try:
            artifacts = _load_artifacts(model_path)
            if not _requires_retrain(artifacts, dataset_hash):
                print("Model loaded from file (dataset hash unchanged)")
                _update_metrics_from_artifacts(artifacts)
                _print_metrics()
                return artifacts
        except Exception as ex:
            print(f"Model load error. Retraining from scratch: {type(ex).__name__}: {ex}")

    artifacts = _train_artifacts(training_df, dataset_hash, csv_path)
    _save_artifacts(model_path, artifacts)
    print("Model trained and saved")
    _update_metrics_from_artifacts(artifacts)
    _print_metrics()
    return artifacts


def predict(artifacts: Dict[str, Any], X: pd.DataFrame) -> np.ndarray:
    if artifacts.get("pipeline") is not None:
        pipeline: Pipeline = artifacts["pipeline"]
        prepared_X = _prepare_inference_dataframe(X)
        predicted_log = pipeline.predict(prepared_X)
        predicted_price = np.expm1(predicted_log)
        predicted_price = np.clip(predicted_price, MIN_PRICE, MAX_PRICE)
        return predicted_price.astype(float)

    if {"model", "scaler", "encoder"}.issubset(artifacts.keys()):
        model = artifacts["model"]
        scaler = artifacts["scaler"]
        encoder = artifacts["encoder"]

        numeric_features = tuple(artifacts.get("numeric_features") or ("year", "mileage"))
        categorical_features = tuple(artifacts.get("categorical_features") or ("fuelType", "transmission"))

        prepared_X = _prepare_inference_dataframe(X)
        X_num = scaler.transform(prepared_X[list(numeric_features)])
        X_cat = encoder.transform(prepared_X[list(categorical_features)])

        if sparse is not None and sparse.issparse(X_cat):
            X_num = sparse.csr_matrix(X_num)
            features = sparse.hstack([X_num, X_cat], format="csr")
        else:
            features = np.hstack([X_num, X_cat])

        predicted_price = model.predict(features)
        predicted_price = np.clip(predicted_price, MIN_PRICE, MAX_PRICE)
        return predicted_price.astype(float)

    raise ValueError("Unsupported model artifacts format.")


def get_metrics() -> Dict[str, Any]:
    return {
        "mae": MODEL_MAE,
        "rmse": MODEL_RMSE,
        "r2": MODEL_R2,
        "modelVersion": MODEL_VERSION,
        "modelType": MODEL_TYPE,
        "trainedAtUtc": MODEL_TRAINED_AT,
    }


def get_prediction_context() -> Dict[str, str]:
    return {
        "confidenceHint": _confidence_from_r2(MODEL_R2),
        "modelVersion": MODEL_VERSION or MODEL_ARTIFACT_VERSION,
    }


def _train_artifacts(training_df: pd.DataFrame, dataset_hash: str, csv_path: str) -> Dict[str, Any]:
    X = training_df[list(MODEL_FEATURES)]
    y_log = np.log1p(training_df["price"].astype(float))

    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y_log,
        test_size=TEST_SIZE,
        random_state=RANDOM_STATE,
    )

    preprocessor = ColumnTransformer(
        transformers=[
            ("num", StandardScaler(), list(NUMERIC_FEATURES)),
            ("cat", OneHotEncoder(handle_unknown="ignore"), list(CATEGORICAL_FEATURES)),
        ]
    )

    model_type, regressor = _build_regressor()
    pipeline = Pipeline(
        steps=[
            ("preprocess", preprocessor),
            ("regressor", regressor),
        ]
    )

    start = time.perf_counter()
    pipeline.fit(X_train, y_train)
    training_time_seconds = float(time.perf_counter() - start)

    predicted_log = pipeline.predict(X_test)
    y_true = np.expm1(y_test.to_numpy(dtype=float))
    y_pred = np.expm1(np.asarray(predicted_log, dtype=float))
    y_pred = np.clip(y_pred, MIN_PRICE, MAX_PRICE)

    mae = float(mean_absolute_error(y_true, y_pred))
    rmse = float(np.sqrt(mean_squared_error(y_true, y_pred)))
    r2 = float(r2_score(y_true, y_pred))

    feature_count = _feature_count_after_encoding(pipeline, X_train)
    dataset_size = int(len(training_df))
    trained_at = datetime.now(timezone.utc).isoformat()

    print(f"Dataset size: {dataset_size}")
    print(f"Feature count: {feature_count}")
    print(f"Training time seconds: {training_time_seconds:.3f}")

    return {
        "version": MODEL_ARTIFACT_VERSION,
        "pipeline": pipeline,
        "model_type": model_type,
        "numeric_features": NUMERIC_FEATURES,
        "categorical_features": CATEGORICAL_FEATURES,
        "metrics": {
            "mae": mae,
            "rmse": rmse,
            "r2": r2,
            "dataset_size": dataset_size,
            "feature_count": feature_count,
            "training_time_seconds": training_time_seconds,
        },
        "metadata": {
            "dataset_hash": dataset_hash,
            "dataset_name": os.path.basename(csv_path),
            "trained_at_utc": trained_at,
            "model_version": MODEL_ARTIFACT_VERSION,
        },
        "target_transform": "log1p",
    }


def _prepare_training_dataframe(df: pd.DataFrame) -> pd.DataFrame:
    working = _ensure_columns(df, include_price=True).copy()
    current_year = datetime.now(timezone.utc).year

    working["year"] = pd.to_numeric(working["year"], errors="coerce")
    working["mileage"] = pd.to_numeric(working["mileage"], errors="coerce")
    working["price"] = pd.to_numeric(working["price"], errors="coerce")

    working = working.dropna(subset=["year", "mileage", "price"])
    working["year"] = working["year"].astype(int)
    working["mileage"] = working["mileage"].astype(float)
    working["price"] = working["price"].astype(float)

    working = working[
        (working["price"] > 0)
        & (working["mileage"] >= 0)
        & (working["year"] >= MIN_YEAR)
        & (working["year"] <= current_year)
    ]

    working["mileage"] = np.clip(working["mileage"], 0, MAX_MILEAGE)
    working["price"] = np.clip(working["price"], MIN_PRICE, MAX_PRICE)

    working["brand"] = working["brand"].map(_normalize_brand)
    working["fuelType"] = working["fuelType"].map(_normalize_fuel_type)
    working["transmission"] = working["transmission"].map(_normalize_transmission)
    working["condition"] = working["condition"].map(_normalize_condition)

    working = _add_derived_features(working, current_year)

    ordered_columns = list(dict.fromkeys(HASH_COLUMNS + MODEL_FEATURES))
    return working[ordered_columns]


def _prepare_inference_dataframe(df: pd.DataFrame) -> pd.DataFrame:
    working = _ensure_columns(df, include_price=False).copy()
    current_year = datetime.now(timezone.utc).year

    working["year"] = pd.to_numeric(working["year"], errors="coerce").fillna(current_year - 5)
    working["mileage"] = pd.to_numeric(working["mileage"], errors="coerce").fillna(0)

    working["year"] = np.clip(working["year"], MIN_YEAR, current_year).astype(int)
    working["mileage"] = np.clip(working["mileage"], 0, MAX_MILEAGE).astype(float)

    working["brand"] = working["brand"].map(_normalize_brand)
    working["fuelType"] = working["fuelType"].map(_normalize_fuel_type)
    working["transmission"] = working["transmission"].map(_normalize_transmission)
    working["condition"] = working["condition"].map(_normalize_condition)

    working = _add_derived_features(working, current_year)

    return working[list(MODEL_FEATURES)]


def _add_derived_features(df: pd.DataFrame, current_year: int) -> pd.DataFrame:
    car_age = np.maximum(current_year - df["year"].astype(float), 0.0)
    car_age_non_zero = np.maximum(car_age, 1.0)

    df["car_age"] = car_age
    df["mileage_per_year"] = df["mileage"] / car_age_non_zero
    df["mileage_per_year"] = np.clip(df["mileage_per_year"], 0.0, 120_000.0)
    df["brand_tier"] = df["brand"].map(_brand_tier_from_brand)
    df["condition_score"] = df["condition"].map(CONDITION_SCORE_BY_VALUE).fillna(0.70)
    return df


def _ensure_columns(df: pd.DataFrame, include_price: bool) -> pd.DataFrame:
    if include_price:
        missing_required = REQUIRED_COLUMNS - set(df.columns)
        if missing_required:
            missing_list = ", ".join(sorted(missing_required))
            raise ValueError(f"Dataset is missing required columns: {missing_list}")

    for column, default_value in DEFAULT_VALUES.items():
        if column not in df.columns:
            df[column] = default_value
        else:
            df[column] = df[column].fillna(default_value)

    if include_price and "price" in df.columns:
        df["price"] = df["price"].fillna(0)

    return df


def _normalize_key(value: Any) -> str:
    return str(value or "").strip().lower().replace("-", "").replace(" ", "").replace("_", "")


def _normalize_brand(value: Any) -> str:
    text = str(value or "").strip()
    return text if text else "Unknown"


def _normalize_fuel_type(value: Any) -> str:
    return FUEL_ALIASES.get(_normalize_key(value), "Petrol")


def _normalize_transmission(value: Any) -> str:
    return TRANSMISSION_ALIASES.get(_normalize_key(value), "Manual")


def _normalize_condition(value: Any) -> str:
    return CONDITION_ALIASES.get(_normalize_key(value), "used")


def _brand_tier_from_brand(brand: str) -> str:
    return BRAND_TIER_BY_KEY.get(str(brand).strip().lower(), "budget")


def _build_regressor():
    if XGBRegressor is not None:
        return (
            "xgboost",
            XGBRegressor(
                n_estimators=400,
                max_depth=8,
                learning_rate=0.05,
                subsample=0.90,
                colsample_bytree=0.90,
                objective="reg:squarederror",
                random_state=RANDOM_STATE,
                n_jobs=1,
            ),
        )

    return (
        "random_forest",
        RandomForestRegressor(
            n_estimators=400,
            max_depth=10,
            min_samples_split=4,
            min_samples_leaf=2,
            random_state=RANDOM_STATE,
            n_jobs=1,
        ),
    )


def _feature_count_after_encoding(pipeline: Pipeline, X_train: pd.DataFrame) -> int:
    preprocessor: ColumnTransformer = pipeline.named_steps["preprocess"]
    transformed = preprocessor.transform(X_train.head(1))
    return int(transformed.shape[1])


def _compute_dataset_hash(df: pd.DataFrame) -> str:
    hash_frame = df[list(HASH_COLUMNS)].sort_values(list(HASH_COLUMNS)).reset_index(drop=True)
    payload = hash_frame.to_csv(index=False).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def _save_artifacts(model_path: str, artifacts: Dict[str, Any]) -> None:
    os.makedirs(os.path.dirname(model_path), exist_ok=True)
    joblib.dump(artifacts, model_path)


def _load_artifacts(model_path: str) -> Dict[str, Any]:
    obj = joblib.load(model_path)

    if isinstance(obj, dict):
        return obj

    if hasattr(obj, "named_steps") and hasattr(obj, "predict"):
        return {
            "version": "legacy-pipeline",
            "pipeline": obj,
            "metrics": {},
            "metadata": {},
        }

    raise ValueError("Unsupported model file format")


def _requires_retrain(artifacts: Dict[str, Any], dataset_hash: str) -> bool:
    if artifacts.get("pipeline") is None:
        return True

    if artifacts.get("version") != MODEL_ARTIFACT_VERSION:
        return True

    metadata = artifacts.get("metadata") or {}
    if metadata.get("dataset_hash") != dataset_hash:
        return True

    if tuple(artifacts.get("numeric_features") or ()) != NUMERIC_FEATURES:
        return True

    if tuple(artifacts.get("categorical_features") or ()) != CATEGORICAL_FEATURES:
        return True

    return False


def _update_metrics_from_artifacts(artifacts: Dict[str, Any]) -> None:
    global MODEL_MAE, MODEL_RMSE, MODEL_R2, MODEL_VERSION, MODEL_TYPE, MODEL_TRAINED_AT

    metrics = artifacts.get("metrics") or {}
    metadata = artifacts.get("metadata") or {}

    mae = metrics.get("mae")
    rmse = metrics.get("rmse")
    r2 = metrics.get("r2")

    MODEL_MAE = float(mae) if mae is not None else None
    MODEL_RMSE = float(rmse) if rmse is not None else None
    MODEL_R2 = float(r2) if r2 is not None else None
    MODEL_VERSION = str(metadata.get("model_version") or artifacts.get("version") or MODEL_ARTIFACT_VERSION)
    MODEL_TYPE = str(artifacts.get("model_type") or "unknown")
    MODEL_TRAINED_AT = str(metadata.get("trained_at_utc") or "")


def _print_metrics() -> None:
    if MODEL_MAE is not None:
        print(f"Model MAE: {MODEL_MAE:.2f}")
    if MODEL_RMSE is not None:
        print(f"Model RMSE: {MODEL_RMSE:.2f}")
    if MODEL_R2 is not None:
        print(f"Model R2: {MODEL_R2:.4f}")


def _confidence_from_r2(r2_value: Optional[float]) -> str:
    if r2_value is None:
        return "low"

    if r2_value >= 0.85:
        return "high"

    if r2_value >= 0.75:
        return "medium"

    return "low"
