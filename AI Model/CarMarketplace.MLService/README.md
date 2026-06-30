# 🤖 AutoMarket AI Model

A Flask-based used-car pricing service that trains a regression model on a local dataset and exposes prediction and metrics endpoints.

---

## 📖 Overview

This project implements a Python ML microservice that generates or loads a car pricing dataset, trains a regression model, persists the artifact, and serves price predictions via a REST API.

---

## ✨ Features

- ✅ Synthetic dataset generation
- ✅ Data validation and preprocessing
- ✅ Regression model training
- ✅ Model serialization to `models/car_price_model.pkl`
- ✅ Flask REST API with `/predict`, `/metrics`, and `/health`
- ✅ Prediction input normalization and output formatting

---

## 🛠 Tech Stack

| Category | Technology |
|----------|------------|
| Language | Python |
| Web Framework | Flask |
| ML Library | scikit-learn |
| Data Processing | pandas |
| Numerical Computing | NumPy |
| Model Serialization | joblib |

---

## 📂 Project Structure

- `app.py` — Flask service entrypoint and prediction API.
- `requirements.txt` — Python dependencies.
- `README.md` — Project documentation.
- `data/`
  - `cars_dataset.csv` — synthetic dataset used for training.
  - `car_prices.csv` — legacy dataset fallback.
- `ml/`
  - `dataset_generator.py` — synthetic dataset generator.
  - `training.py` — data preparation, model training, inference, and metrics.
- `models/`
  - `car_price_model.pkl` — persisted trained model artifact.

---

## ⚙️ Installation

```bash
git clone <repo-url>
cd "CarMarketplace.MLService"
python -m venv .venv
.\.venv\Scripts\activate
pip install -r requirements.txt
python app.py
```

If you want to generate the synthetic dataset manually:

```bash
python ml/dataset_generator.py --rows 12000 --seed 42 --output data/cars_dataset.csv
```

---

## 📊 Model Information

- The service trains on `data/cars_dataset.csv` by default.
- If that dataset is missing, it can auto-generate a synthetic dataset or fall back to `data/car_prices.csv`.
- Training includes:
  - cleaning and filtering `year`, `mileage`, and `price`
  - normalizing `brand`, `fuelType`, `transmission`, and `condition`
  - deriving features such as `car_age`, `mileage_per_year`, `brand_tier`, and `condition_score`
- The model pipeline uses scaling, one-hot encoding, and a regression estimator:
  - `XGBRegressor` when available
  - otherwise `RandomForestRegressor`
- Predictions are returned as JSON with `predictedPrice`, `confidenceHint`, and `modelVersion`.

---

## 🔗 Backend Integration

The backend is implemented in `app.py` as a Flask application. It loads or trains the model on startup and provides:

- `GET /health`
- `GET /metrics`
- `POST /predict`

Prediction requests require:

```json
{
  "year": 2018,
  "mileage": 45000,
  "fuelType": "Gasoline",
  "transmission": "Manual"
}
```

Optional fields include `brand` and `condition`.

---

## 🚀 Future Improvements

- Add unit and integration tests for the API and training pipeline
- Add Docker support for reproducible deployment
- Add explicit model version metadata and artifact tracking
- Improve endpoint schema validation and error reporting

---

## 🤝 Contributing

Contributions are welcome. Please open an issue or submit a pull request with code improvements, bug fixes, or documentation updates.

---

## 📄 License

MIT License.
