"""
Charts Router
API endpoints for chart data with timing metrics
"""
import time
from typing import Optional
from fastapi import APIRouter, Query, Request
from fastapi.responses import PlainTextResponse, JSONResponse

from services.data_generator import create_generator
from services.metrics_logger import MetricsLogger, TimingContext

router = APIRouter()
logger = MetricsLogger("charts_router")


@router.get("/boxplot")
async def get_boxplot_data(
    request: Request,
    year: int = Query(2025, description="Production year"),
    weeks: int = Query(52, ge=1, description="Number of weeks"),
    lots_per_week: int = Query(10, ge=1, description="Lots per week"),
    wafers_per_lot: int = Query(25, ge=1, description="Wafers per lot")
):
    """
    Get box plot data for semiconductor yield visualization
    
    Returns hierarchical data structure with:
    - Metadata (totals, configuration)
    - Hierarchy definition (Year → Week → Lot)
    - Weekly data with lot statistics
    
    Response headers include timing metrics:
    - X-Data-Generation-Ms
    - X-Serialization-Ms
    - X-Server-Timing
    """
    total_timing = {}
    
    # Get request start time from middleware
    request_start = getattr(request.state, 'start_time', time.perf_counter())
    
    # Generate data
    with TimingContext(logger, "boxplot_endpoint") as endpoint_timer:
        generator = create_generator(
            year=year,
            weeks=weeks,
            lots_per_week=lots_per_week,
            wafers_per_lot=wafers_per_lot
        )
        
        boxplot_data, gen_metrics = generator.generate_boxplot_data()
        total_timing.update(gen_metrics)
        
        # Prepare response with timing
        with TimingContext(logger, "response_preparation") as prep_timer:
            response_data = {
                "data": boxplot_data,
                "timing": {
                    "server": {
                        **total_timing,
                        "endpoint_total_ms": 0  # Will be updated
                    }
                }
            }
    
    total_timing["endpoint_total_ms"] = endpoint_timer.elapsed_ms
    response_data["timing"]["server"]["endpoint_total_ms"] = endpoint_timer.elapsed_ms
    
    # Create response with timing headers
    response = JSONResponse(content=response_data)
    response.headers["X-Data-Generation-Ms"] = str(round(
        total_timing.get("array_generation_ms", 0) + 
        total_timing.get("yield_generation_ms", 0) +
        total_timing.get("dataframe_creation_ms", 0), 2
    ))
    response.headers["X-Transformation-Ms"] = str(round(
        total_timing.get("boxplot_transformation_ms", 0), 2
    ))
    
    return response


@router.get("/csv", response_class=PlainTextResponse)
async def get_csv_data(
    request: Request,
    year: int = Query(2025, description="Production year"),
    weeks: int = Query(52, ge=1, description="Number of weeks"),
    lots_per_week: int = Query(10, ge=1, description="Lots per week"),
    wafers_per_lot: int = Query(25, ge=1, description="Wafers per lot")
):
    """
    Get raw CSV data for semiconductor yield
    
    CSV columns: Lot_id, Wafer_id, Year, Week_no, Yield
    """
    generator = create_generator(
        year=year,
        weeks=weeks,
        lots_per_week=lots_per_week,
        wafers_per_lot=wafers_per_lot
    )
    
    csv_data, timing_metrics = generator.generate_csv()
    
    logger.log_timing("csv_download", {
        "total_points": timing_metrics.get("total_data_points", 0),
        "size_bytes": timing_metrics.get("csv_size_bytes", 0),
        "generation_ms": timing_metrics.get("dataframe_creation_ms", 0)
    })
    
    response = PlainTextResponse(
        content=csv_data,
        media_type="text/csv"
    )
    response.headers["Content-Disposition"] = f"attachment; filename=yield_data_{year}.csv"
    response.headers["X-Data-Points"] = str(timing_metrics.get("total_data_points", 0))
    response.headers["X-Generation-Ms"] = str(round(
        timing_metrics.get("array_generation_ms", 0) +
        timing_metrics.get("yield_generation_ms", 0) +
        timing_metrics.get("dataframe_creation_ms", 0), 2
    ))
    
    return response


@router.get("/test-config")
async def get_test_config():
    """
    Get available test configurations
    """
    return {
        "parameters": {
            "year": {
                "type": "integer",
                "default": 2025,
                "description": "Production year"
            },
            "weeks": {
                "type": "integer",
                "min": 1,
                "max": 52,
                "default": 52,
                "description": "Number of weeks to generate"
            },
            "lots_per_week": {
                "type": "integer",
                "min": 1,
                "max": 100,
                "default": 10,
                "description": "Number of lots per week"
            },
            "wafers_per_lot": {
                "type": "integer",
                "min": 1,
                "max": 25,
                "default": 25,
                "description": "Wafers per lot (max 25)"
            }
        },
        "presets": {
            "small": {"weeks": 4, "lots_per_week": 5, "wafers_per_lot": 10},
            "medium": {"weeks": 12, "lots_per_week": 10, "wafers_per_lot": 20},
            "large": {"weeks": 52, "lots_per_week": 20, "wafers_per_lot": 25},
            "stress": {"weeks": 52, "lots_per_week": 100, "wafers_per_lot": 25}
        },
        "data_points_formula": "weeks × lots_per_week × wafers_per_lot"
    }


@router.get("/metrics")
async def get_metrics():
    """
    Get aggregated timing metrics from recent requests
    """
    return {
        "metrics": logger.get_metrics(),
        "note": "Metrics are stored in memory and reset on server restart"
    }
