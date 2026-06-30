import argparse
from pathlib import Path
from typing import Dict, Tuple

import numpy as np
import pandas as pd


MIN_YEAR = 1995
MAX_YEAR = 2025
DEFAULT_ROWS = 12_000
DEFAULT_SEED = 42
DEFAULT_OUTPUT = Path(__file__).resolve().parent.parent / "data" / "cars_dataset.csv"

BRAND_CONFIG: Dict[str, Dict[str, object]] = {
    "BMW": {
        "weight": 0.05,
        "tier_multiplier": 1.18,
        "models": {
            "320i": 2_900_000,
            "520i": 4_350_000,
            "X1": 3_300_000,
            "X3": 4_850_000,
        },
    },
    "Mercedes": {
        "weight": 0.04,
        "tier_multiplier": 1.20,
        "models": {
            "C180": 3_450_000,
            "E200": 5_500_000,
            "GLC 200": 5_900_000,
        },
    },
    "Audi": {
        "weight": 0.02,
        "tier_multiplier": 1.15,
        "models": {
            "A3": 2_700_000,
            "A4": 3_550_000,
            "Q3": 3_950_000,
        },
    },
    "Toyota": {
        "weight": 0.13,
        "tier_multiplier": 1.05,
        "models": {
            "Corolla": 1_300_000,
            "Camry": 2_500_000,
            "Yaris": 1_150_000,
            "Fortuner": 3_300_000,
        },
    },
    "Honda": {
        "weight": 0.08,
        "tier_multiplier": 1.04,
        "models": {
            "Civic": 1_650_000,
            "Accord": 2_450_000,
            "CR-V": 2_900_000,
        },
    },
    "Kia": {
        "weight": 0.14,
        "tier_multiplier": 0.95,
        "models": {
            "Cerato": 1_250_000,
            "Sportage": 2_200_000,
            "Rio": 980_000,
        },
    },
    "Hyundai": {
        "weight": 0.16,
        "tier_multiplier": 0.93,
        "models": {
            "Elantra AD": 1_200_000,
            "Tucson": 2_100_000,
            "Accent RB": 850_000,
        },
    },
    "Nissan": {
        "weight": 0.11,
        "tier_multiplier": 0.92,
        "models": {
            "Sunny": 830_000,
            "Qashqai": 1_900_000,
            "X-Trail": 2_500_000,
        },
    },
    "Chevrolet": {
        "weight": 0.10,
        "tier_multiplier": 0.90,
        "models": {
            "Optra": 760_000,
            "Captiva": 1_750_000,
            "Aveo": 680_000,
        },
    },
    "MG": {
        "weight": 0.10,
        "tier_multiplier": 0.94,
        "models": {
            "MG 5": 1_050_000,
            "ZS": 1_450_000,
            "RX5": 1_850_000,
        },
    },
    "Skoda": {
        "weight": 0.07,
        "tier_multiplier": 0.97,
        "models": {
            "Octavia": 1_700_000,
            "Kamiq": 1_900_000,
            "Kodiaq": 2_800_000,
        },
    },
}

LOCATIONS = {
    "Cairo": 0.33,
    "Giza": 0.17,
    "Alexandria": 0.14,
    "Dakahlia": 0.06,
    "Sharqia": 0.06,
    "Gharbia": 0.05,
    "Qalyubia": 0.05,
    "Monufia": 0.04,
    "Ismailia": 0.03,
    "Suez": 0.03,
    "Assiut": 0.02,
    "Luxor": 0.01,
    "Aswan": 0.01,
}

LOCATION_MULTIPLIER = {
    "Cairo": 1.03,
    "Giza": 1.02,
    "Alexandria": 1.01,
    "Dakahlia": 1.00,
    "Sharqia": 1.00,
    "Gharbia": 0.99,
    "Qalyubia": 1.00,
    "Monufia": 0.99,
    "Ismailia": 1.00,
    "Suez": 1.01,
    "Assiut": 0.98,
    "Luxor": 0.98,
    "Aswan": 0.97,
}

CONDITION_MULTIPLIER = {
    "new": 1.10,
    "used": 1.00,
    "very_used": 0.84,
}

TRANSMISSION_MULTIPLIER = {
    "Manual": 1.00,
    "Automatic": 1.03,
}

FUEL_MULTIPLIER = {
    "Petrol": 1.00,
    "Diesel": 1.02,
    "Hybrid": 1.08,
}


def generate_brand(rng: np.random.Generator) -> Tuple[str, str]:
    brands = list(BRAND_CONFIG.keys())
    brand_weights = [BRAND_CONFIG[brand]["weight"] for brand in brands]
    brand = str(rng.choice(brands, p=brand_weights))
    models = list(BRAND_CONFIG[brand]["models"].keys())
    model = str(rng.choice(models))
    return brand, model


def generate_year(rng: np.random.Generator) -> int:
    year = int(np.rint(rng.triangular(MIN_YEAR, 2019, MAX_YEAR)))
    return int(np.clip(year, MIN_YEAR, MAX_YEAR))


def calculate_price(
    brand: str,
    model: str,
    year: int,
    mileage: int,
    fuel_type: str,
    transmission: str,
    condition: str,
    location: str,
    rng: np.random.Generator,
) -> float:
    brand_info = BRAND_CONFIG[brand]
    base_model_price = float(brand_info["models"][model])
    age = MAX_YEAR - year

    year_factor = max(0.22, 0.95**age)
    mileage_factor = max(0.60, 1.0 - (mileage / 300_000) * 0.40)

    raw_price = (
        base_model_price
        * float(brand_info["tier_multiplier"])
        * year_factor
        * mileage_factor
        * CONDITION_MULTIPLIER[condition]
        * TRANSMISSION_MULTIPLIER[transmission]
        * FUEL_MULTIPLIER[fuel_type]
        * LOCATION_MULTIPLIER[location]
    )

    noisy_price = raw_price * (1.0 + rng.uniform(-0.05, 0.05))
    min_price = max(65_000.0, base_model_price * 0.08)
    max_price = base_model_price * float(brand_info["tier_multiplier"]) * 1.30

    final_price = np.clip(noisy_price, min_price, max_price)
    return round(float(final_price), 2)


def _generate_mileage(year: int, rng: np.random.Generator) -> int:
    age = MAX_YEAR - year

    if age <= 1:
        mileage = rng.normal(8_000, 5_000)
    else:
        annual_km = rng.normal(17_000, 3_500)
        mileage = age * annual_km + rng.normal(0, 12_000)

    mileage = int(np.clip(np.rint(mileage), 0, 300_000))
    return mileage


def _generate_fuel_type(brand: str, year: int, rng: np.random.Generator) -> str:
    if year >= 2020:
        probs = np.array([0.72, 0.10, 0.18])
    elif year >= 2012:
        probs = np.array([0.80, 0.16, 0.04])
    else:
        probs = np.array([0.86, 0.13, 0.01])

    if brand in {"Toyota", "Honda"}:
        probs[2] += 0.06
        probs[0] -= 0.06

    probs = probs / probs.sum()
    return str(rng.choice(["Petrol", "Diesel", "Hybrid"], p=probs))


def _generate_transmission(brand: str, year: int, rng: np.random.Generator) -> str:
    auto_probability = 0.65

    if brand in {"BMW", "Mercedes", "Audi"}:
        auto_probability = 0.95
    elif brand in {"Toyota", "Honda", "Skoda"}:
        auto_probability = 0.82
    elif brand in {"Kia", "Hyundai", "Nissan", "MG"}:
        auto_probability = 0.72

    if year <= 2008:
        auto_probability -= 0.20
    elif year >= 2020:
        auto_probability += 0.08

    auto_probability = float(np.clip(auto_probability, 0.20, 0.98))
    return str(rng.choice(["Manual", "Automatic"], p=[1 - auto_probability, auto_probability]))


def _generate_condition(year: int, mileage: int, rng: np.random.Generator) -> str:
    age = MAX_YEAR - year
    usage_score = age * 6 + mileage / 12_000

    if usage_score < 18:
        probs = [0.70, 0.28, 0.02]
    elif usage_score < 45:
        probs = [0.06, 0.82, 0.12]
    else:
        probs = [0.01, 0.48, 0.51]

    return str(rng.choice(["new", "used", "very_used"], p=probs))


def _generate_location(rng: np.random.Generator) -> str:
    names = list(LOCATIONS.keys())
    weights = list(LOCATIONS.values())
    return str(rng.choice(names, p=weights))


def generate_dataset(
    num_rows: int = DEFAULT_ROWS,
    seed: int = DEFAULT_SEED,
    output_path: Path = DEFAULT_OUTPUT,
) -> pd.DataFrame:
    rng = np.random.default_rng(seed)
    rows = []

    for _ in range(num_rows):
        brand, model = generate_brand(rng)
        year = generate_year(rng)
        mileage = _generate_mileage(year, rng)
        fuel_type = _generate_fuel_type(brand, year, rng)
        transmission = _generate_transmission(brand, year, rng)
        condition = _generate_condition(year, mileage, rng)
        location = _generate_location(rng)

        price = calculate_price(
            brand=brand,
            model=model,
            year=year,
            mileage=mileage,
            fuel_type=fuel_type,
            transmission=transmission,
            condition=condition,
            location=location,
            rng=rng,
        )

        rows.append(
            {
                "brand": brand,
                "model": model,
                "year": year,
                "mileage": mileage,
                "fuelType": fuel_type,
                "transmission": transmission,
                "condition": condition,
                "location": location,
                "price": price,
            }
        )

    dataset = pd.DataFrame(rows)

    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    dataset.to_csv(output_path, index=False)
    return dataset


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Generate synthetic used-car dataset for Egypt marketplace."
    )
    parser.add_argument(
        "--rows",
        type=int,
        default=DEFAULT_ROWS,
        help=f"Number of rows to generate (default: {DEFAULT_ROWS}).",
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=DEFAULT_SEED,
        help=f"Random seed for reproducibility (default: {DEFAULT_SEED}).",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help=f"CSV output path (default: {DEFAULT_OUTPUT}).",
    )

    args = parser.parse_args()
    df = generate_dataset(num_rows=args.rows, seed=args.seed, output_path=args.output)

    print(f"Generated {len(df):,} rows -> {args.output.resolve()}")
    print(
        "Price range (EGP): "
        f"{df['price'].min():,.0f} - {df['price'].max():,.0f} | "
        f"Mean: {df['price'].mean():,.0f}"
    )
