# Chart Testing Framework - Backend

## Setup
```bash
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

## API Endpoints
- `GET /api/charts/boxplot` - Box plot data with hierarchical structure
- `GET /api/charts/csv` - Raw CSV download
- `GET /api/health` - Health check
