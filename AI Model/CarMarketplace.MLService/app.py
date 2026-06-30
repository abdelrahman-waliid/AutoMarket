import datetime
import os
from pathlib import Path

import pandas as pd
from flask import Flask, jsonify, request

try:
    from ml import training  # type: ignore
except Exception as ex:  # pragma: no cover
    training = None
    _training_import_error = f"{type(ex).__name__}: {ex}"
    print(f"ML module import error: {_training_import_error}")

try:
    from ml import dataset_generator  # type: ignore
except Exception as ex:  # pragma: no cover
    dataset_generator = None
    _dataset_generator_import_error = f"{type(ex).__name__}: {ex}"
    print(f"Dataset generator import error: {_dataset_generator_import_error}")


DEFAULT_DATASET_ROWS = 12_000
DEFAULT_DATASET_SEED = 42
MIN_SYNTHETIC_DATASET_ROWS = 10_000

ALLOWED_FUEL_TYPES = {"Petrol", "Diesel", "Hybrid"}
ALLOWED_TRANSMISSIONS = {"Manual", "Automatic"}
ALLOWED_CONDITIONS = {"new", "used", "refurbished"}

FUEL_TYPE_ALIASES = {
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


def _normalize_category_key(value: str) -> str:
    return value.strip().lower().replace("-", "").replace(" ", "").replace("_", "")


def _to_int_env(name: str, default: int) -> int:
    value = os.environ.get(name)
    if value is None:
        return default

    try:
        return int(value)
    except ValueError:
        print(f"Invalid {name} value '{value}', using default {default}.")
        return default


def _resolve_dataset_path(service_root: str) -> str:
    configured_path = os.environ.get("ML_DATASET_PATH")
    if configured_path:
        return os.path.abspath(configured_path)

    data_dir = os.path.join(service_root, "data")
    synthetic_dataset = os.path.join(data_dir, "cars_dataset.csv")
    legacy_dataset = os.path.join(data_dir, "car_prices.csv")

    if os.path.exists(synthetic_dataset):
        return synthetic_dataset

    if os.path.exists(legacy_dataset):
        return legacy_dataset

    return synthetic_dataset


def _ensure_training_dataset(dataset_path: str) -> None:
    if os.path.exists(dataset_path):
        return

    if dataset_generator is not None:
        rows = max(_to_int_env("ML_DATASET_ROWS", DEFAULT_DATASET_ROWS), MIN_SYNTHETIC_DATASET_ROWS)
        seed = max(_to_int_env("ML_DATASET_SEED", DEFAULT_DATASET_SEED), 0)
        dataset_generator.generate_dataset(num_rows=rows, seed=seed, output_path=Path(dataset_path))
        print(f"Synthetic dataset generated at {dataset_path} (rows={rows}, seed={seed}).")
        return

    if training is not None:
        training.ensure_sample_dataset(dataset_path)
        print(f"Fallback sample dataset generated at {dataset_path}.")


def _validate_predict_payload(payload):
    if not isinstance(payload, dict):
        return None, "Request body must be a JSON object."

    required = ("year", "mileage", "fuelType", "transmission")
    missing = [field for field in required if field not in payload]
    if missing:
        return None, f"Missing required field(s): {', '.join(missing)}."

    try:
        year = int(payload.get("year"))
    except (TypeError, ValueError):
        return None, "year must be an integer."

    current_year = datetime.date.today().year
    if year < 1990 or year > current_year:
        return None, f"year must be between 1990 and {current_year}."

    try:
        mileage = int(payload.get("mileage"))
    except (TypeError, ValueError):
        return None, "mileage must be an integer."

    if mileage < 0:
        return None, "mileage must be greater than or equal to 0."

    fuel_type = str(payload.get("fuelType") or "").strip()
    if not fuel_type:
        return None, "fuelType is required."

    fuel_type_canonical = FUEL_TYPE_ALIASES.get(_normalize_category_key(fuel_type))
    if fuel_type_canonical is None:
        allowed = ", ".join(sorted(ALLOWED_FUEL_TYPES))
        return None, f"fuelType must map to one of: {allowed}."

    transmission = str(payload.get("transmission") or "").strip()
    if not transmission:
        return None, "transmission is required."

    transmission_canonical = TRANSMISSION_ALIASES.get(_normalize_category_key(transmission))
    if transmission_canonical is None:
        allowed = ", ".join(sorted(ALLOWED_TRANSMISSIONS))
        return None, f"transmission must map to one of: {allowed}."

    brand = str(payload.get("brand") or "").strip()
    if not brand:
        brand = "Unknown"

    condition_raw = str(payload.get("condition") or "used").strip()
    condition_canonical = CONDITION_ALIASES.get(_normalize_category_key(condition_raw))
    if condition_canonical is None:
        allowed = ", ".join(sorted(ALLOWED_CONDITIONS))
        return None, f"condition must map to one of: {allowed}."

    return (
        {
            "brand": brand,
            "year": year,
            "mileage": mileage,
            "fuelType": fuel_type_canonical,
            "transmission": transmission_canonical,
            "condition": condition_canonical,
        },
        None,
    )


def create_app() -> Flask:
    service_root = os.path.dirname(os.path.abspath(__file__))
    dataset_path = _resolve_dataset_path(service_root)
    model_path = os.path.join(service_root, "models", "car_price_model.pkl")

    app = Flask(__name__)

    model_artifacts = None
    model_error = None
    if training is None:
        model_error = "ML dependencies are not available."
    else:
        try:
            _ensure_training_dataset(dataset_path)
            model_artifacts = training.load_or_train_model(dataset_path, model_path)
            print(f"Model dataset path: {dataset_path}")
        except Exception as ex:
            model_error = f"{type(ex).__name__}: {ex}"
            print(f"Model startup error: {model_error}")

    @app.get("/health")
    def health():
        return jsonify({"status": "ok"})

    @app.get("/metrics")
    def metrics():
        if model_artifacts is None or training is None:
            return jsonify({"error": "Model error", "details": model_error or "Model is not loaded."}), 500

        metrics_payload = training.get_metrics()
        if metrics_payload.get("mae") is None or metrics_payload.get("r2") is None:
            return jsonify({"error": "Model error", "details": "Metrics are not available."}), 500

        return jsonify(
            {
                "mae": metrics_payload["mae"],
                "rmse": metrics_payload.get("rmse"),
                "r2": metrics_payload["r2"],
                "modelVersion": metrics_payload.get("modelVersion"),
                "modelType": metrics_payload.get("modelType"),
            }
        )

    @app.post("/predict")
    def predict():
        payload = request.get_json(silent=True)
        parsed, error_details = _validate_predict_payload(payload)
        if error_details is not None:
            return jsonify({"error": "Invalid input", "details": error_details}), 400

        if model_artifacts is None or training is None:
            return jsonify({"error": "Model error", "details": model_error or "Model is not loaded."}), 500

        X = pd.DataFrame([parsed])

        try:
            predicted = float(training.predict(model_artifacts, X)[0])
        except Exception as ex:
            return jsonify({"error": "Model error", "details": f"Prediction failed: {type(ex).__name__}"}), 500

        prediction_context = training.get_prediction_context()
        predicted = round(max(predicted, 50_000.0), 2)

        return jsonify(
            {
                "predictedPrice": predicted,
                "confidenceHint": prediction_context["confidenceHint"],
                "modelVersion": prediction_context["modelVersion"],
            }
        )

    return app


app = create_app()


if __name__ == "__main__":
    host = os.environ.get("HOST", "0.0.0.0")
    port = int(os.environ.get("PORT", "5000"))
    app.run(host=host, port=port)
