"""
Chart Testing Framework - FastAPI Backend
Provides chart data with comprehensive timing metrics
"""
import time
from contextlib import asynccontextmanager
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from routers import charts
from services.metrics_logger import MetricsLogger

logger = MetricsLogger("main")


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan events"""
    logger.info("Chart Testing Framework Backend starting...")
    yield
    logger.info("Backend shutting down...")


app = FastAPI(
    title="Chart Testing Framework API",
    description="Backend for testing chart performance with large datasets",
    version="1.0.0",
    lifespan=lifespan
)

# CORS for Blazor frontends
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
    expose_headers=["X-Server-Timing", "X-Data-Generation-Ms", "X-Serialization-Ms"]
)


@app.middleware("http")
async def timing_middleware(request: Request, call_next):
    """Add server timing headers to all responses"""
    start_time = time.perf_counter()
    request.state.start_time = start_time
    
    response = await call_next(request)
    
    total_time_ms = (time.perf_counter() - start_time) * 1000
    response.headers["X-Server-Timing"] = f"total;dur={total_time_ms:.2f}"
    
    logger.log_timing("request_complete", {
        "path": request.url.path,
        "method": request.method,
        "total_ms": round(total_time_ms, 2)
    })
    
    return response


# Register routers
app.include_router(charts.router, prefix="/api/charts", tags=["Charts"])


@app.get("/api/health")
async def health_check():
    """Health check endpoint"""
    return {"status": "healthy", "service": "chart-testing-backend"}


@app.get("/")
async def root():
    """Root endpoint with API info"""
    return {
        "name": "Chart Testing Framework API",
        "version": "1.0.0",
        "endpoints": {
            "boxplot": "/api/charts/boxplot",
            "csv": "/api/charts/csv",
            "health": "/api/health"
        }
    }
