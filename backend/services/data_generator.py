"""
Data Generator Service
Generates semiconductor yield data for box plot testing
"""
import time
import io
from typing import Dict, List, Any, Optional
import numpy as np
import pandas as pd

from services.metrics_logger import MetricsLogger, TimingContext

logger = MetricsLogger("data_generator")


class SemiconductorDataGenerator:
    """
    Generates mock semiconductor yield data
    
    Data structure:
    - Lot_id: Unique lot identifier (e.g., L001)
    - Wafer_id: Wafer within lot (e.g., W01-W25)
    - Year: Production year
    - Week_no: Week number (1-52)
    - Yield: Yield percentage (0-100)
    """
    
    def __init__(
        self,
        year: int = 2025,
        weeks_per_year: int = 52,
        lots_per_week: int = 10,
        wafers_per_lot: int = 25
    ):
        self.year = year
        self.weeks_per_year = weeks_per_year
        self.lots_per_week = lots_per_week
        self.wafers_per_lot = wafers_per_lot  # No limit
        
        # Yield distribution parameters (realistic semiconductor yields)
        self.yield_mean = 92.0
        self.yield_std = 4.0
        self.yield_min = 70.0
        self.yield_max = 99.5
    
    def generate_dataframe(self) -> tuple[pd.DataFrame, Dict[str, float]]:
        """
        Generate complete dataset as pandas DataFrame
        Returns: (DataFrame, timing_metrics)
        """
        timing_metrics = {}
        
        # Calculate total data points
        total_lots = self.weeks_per_year * self.lots_per_week
        total_points = total_lots * self.wafers_per_lot
        
        logger.info(f"Generating {total_points:,} data points ({total_lots} lots)")
        
        # Generate data using vectorized operations
        with TimingContext(logger, "array_generation") as timer:
            # Create lot IDs
            lot_ids = []
            wafer_ids = []
            years = []
            week_nos = []
            
            lot_counter = 1
            for week in range(1, self.weeks_per_year + 1):
                for _ in range(self.lots_per_week):
                    lot_id = f"L{lot_counter:04d}"
                    for wafer in range(1, self.wafers_per_lot + 1):
                        lot_ids.append(lot_id)
                        wafer_ids.append(f"W{wafer:02d}")
                        years.append(self.year)
                        week_nos.append(week)
                    lot_counter += 1
        
        timing_metrics["array_generation_ms"] = timer.elapsed_ms
        
        # Generate yields with realistic distribution
        with TimingContext(logger, "yield_generation") as timer:
            yields = np.random.normal(self.yield_mean, self.yield_std, total_points)
            yields = np.clip(yields, self.yield_min, self.yield_max)
            yields = np.round(yields, 2)
        
        timing_metrics["yield_generation_ms"] = timer.elapsed_ms
        
        # Create DataFrame
        with TimingContext(logger, "dataframe_creation") as timer:
            df = pd.DataFrame({
                "Lot_id": lot_ids,
                "Wafer_id": wafer_ids,
                "Year": years,
                "Week_no": week_nos,
                "Yield": yields
            })
        
        timing_metrics["dataframe_creation_ms"] = timer.elapsed_ms
        timing_metrics["total_data_points"] = total_points
        
        return df, timing_metrics
    
    def generate_boxplot_data(self) -> tuple[Dict[str, Any], Dict[str, float]]:
        """
        Generate box plot formatted data with hierarchical structure
        Returns: (boxplot_data, timing_metrics)
        """
        timing_metrics = {}
        
        # Generate base DataFrame
        df, gen_metrics = self.generate_dataframe()
        timing_metrics.update(gen_metrics)
        
        # Transform to box plot structure
        with TimingContext(logger, "boxplot_transformation") as timer:
            boxplot_data = {
                "metadata": {
                    "year": self.year,
                    "total_weeks": self.weeks_per_year,
                    "total_lots": self.weeks_per_year * self.lots_per_week,
                    "wafers_per_lot": self.wafers_per_lot,
                    "total_points": len(df)
                },
                "hierarchy": {
                    "levels": ["Year", "Week_no", "Lot_id"],
                    "years": [self.year]
                },
                "weeks": []
            }
            
            # Group by week and lot
            for week in range(1, self.weeks_per_year + 1):
                week_data = df[df["Week_no"] == week]
                week_entry = {
                    "week_no": week,
                    "lots": []
                }
                
                for lot_id in week_data["Lot_id"].unique():
                    lot_data = week_data[week_data["Lot_id"] == lot_id]
                    yields = lot_data["Yield"].tolist()
                    
                    # Calculate box plot statistics
                    lot_entry = {
                        "lot_id": lot_id,
                        "wafers": lot_data[["Wafer_id", "Yield"]].to_dict("records"),
                        "stats": {
                            "min": float(np.min(yields)),
                            "q1": float(np.percentile(yields, 25)),
                            "median": float(np.median(yields)),
                            "q3": float(np.percentile(yields, 75)),
                            "max": float(np.max(yields)),
                            "mean": float(np.mean(yields)),
                            "count": len(yields)
                        }
                    }
                    week_entry["lots"].append(lot_entry)
                
                boxplot_data["weeks"].append(week_entry)
        
        timing_metrics["boxplot_transformation_ms"] = timer.elapsed_ms
        
        return boxplot_data, timing_metrics
    
    def generate_csv(self) -> tuple[str, Dict[str, float]]:
        """
        Generate CSV string
        Returns: (csv_string, timing_metrics)
        """
        df, timing_metrics = self.generate_dataframe()
        
        with TimingContext(logger, "csv_serialization") as timer:
            csv_buffer = io.StringIO()
            df.to_csv(csv_buffer, index=False)
            csv_string = csv_buffer.getvalue()
        
        timing_metrics["csv_serialization_ms"] = timer.elapsed_ms
        timing_metrics["csv_size_bytes"] = len(csv_string.encode('utf-8'))
        
        return csv_string, timing_metrics


def create_generator(
    year: int = 2025,
    weeks: int = 52,
    lots_per_week: int = 10,
    wafers_per_lot: int = 25
) -> SemiconductorDataGenerator:
    """Factory function to create data generator with custom parameters"""
    return SemiconductorDataGenerator(
        year=year,
        weeks_per_year=weeks,
        lots_per_week=lots_per_week,
        wafers_per_lot=wafers_per_lot
    )
