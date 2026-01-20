"""
Metrics Logger Service
Provides timing decorators and structured logging for performance measurement
"""
import time
import json
import logging
import functools
from datetime import datetime
from typing import Any, Callable, Dict, Optional

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s | %(name)s | %(levelname)s | %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)


class MetricsLogger:
    """Centralized logging service with timing capabilities"""
    
    def __init__(self, name: str):
        self.logger = logging.getLogger(name)
        self.metrics_store: Dict[str, list] = {}
    
    def info(self, message: str):
        """Log info message"""
        self.logger.info(message)
    
    def error(self, message: str):
        """Log error message"""
        self.logger.error(message)
    
    def log_timing(self, operation: str, metrics: Dict[str, Any]):
        """Log timing metrics in structured format"""
        log_entry = {
            "timestamp": datetime.utcnow().isoformat(),
            "operation": operation,
            **metrics
        }
        self.logger.info(f"TIMING | {json.dumps(log_entry)}")
        
        # Store for aggregation
        if operation not in self.metrics_store:
            self.metrics_store[operation] = []
        self.metrics_store[operation].append(metrics)
    
    def get_metrics(self, operation: Optional[str] = None) -> Dict[str, list]:
        """Retrieve stored metrics"""
        if operation:
            return {operation: self.metrics_store.get(operation, [])}
        return self.metrics_store
    
    def clear_metrics(self):
        """Clear stored metrics"""
        self.metrics_store = {}


def timed(logger: MetricsLogger, operation_name: str):
    """
    Decorator to time function execution
    
    Usage:
        @timed(logger, "data_generation")
        def generate_data():
            ...
    """
    def decorator(func: Callable):
        @functools.wraps(func)
        async def async_wrapper(*args, **kwargs):
            start = time.perf_counter()
            result = await func(*args, **kwargs)
            elapsed_ms = (time.perf_counter() - start) * 1000
            
            logger.log_timing(operation_name, {
                "function": func.__name__,
                "duration_ms": round(elapsed_ms, 2)
            })
            return result
        
        @functools.wraps(func)
        def sync_wrapper(*args, **kwargs):
            start = time.perf_counter()
            result = func(*args, **kwargs)
            elapsed_ms = (time.perf_counter() - start) * 1000
            
            logger.log_timing(operation_name, {
                "function": func.__name__,
                "duration_ms": round(elapsed_ms, 2)
            })
            return result
        
        # Return appropriate wrapper based on function type
        import asyncio
        if asyncio.iscoroutinefunction(func):
            return async_wrapper
        return sync_wrapper
    
    return decorator


class TimingContext:
    """
    Context manager for timing code blocks
    
    Usage:
        with TimingContext(logger, "data_processing") as timer:
            # do work
        print(timer.elapsed_ms)
    """
    
    def __init__(self, logger: MetricsLogger, operation_name: str):
        self.logger = logger
        self.operation_name = operation_name
        self.start_time: float = 0
        self.elapsed_ms: float = 0
    
    def __enter__(self):
        self.start_time = time.perf_counter()
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.elapsed_ms = (time.perf_counter() - self.start_time) * 1000
        self.logger.log_timing(self.operation_name, {
            "duration_ms": round(self.elapsed_ms, 2)
        })
        return False
