import os
from urllib.parse import quote_plus
import pandas as pd
from sqlalchemy import create_engine, text
from dotenv import load_dotenv
from google.cloud import bigquery

load_dotenv()

# ── SQL Server (SQLAlchemy) ──────────────────────────────────────────────────────
_driver   = os.getenv("DB_DRIVER", "ODBC Driver 17 for SQL Server")
_server   = os.getenv("DB_SERVER", r"localhost\SQLEXPRESS")
_database = os.getenv("DB_DATABASE", "DataFlowPlatform")

_odbc = (
    f"DRIVER={{{_driver}}};"
    f"SERVER={_server};"
    f"DATABASE={_database};"
    "Trusted_Connection=yes;"
    "TrustServerCertificate=yes;"
)

_engine = create_engine(
    f"mssql+pyodbc:///?odbc_connect={quote_plus(_odbc)}",
    pool_pre_ping=True,
)


def query_df(sql: str, params: dict | None = None) -> pd.DataFrame:
    with _engine.connect() as conn:
        return pd.read_sql(text(sql), conn, params=params)


# ── BigQuery ─────────────────────────────────────────────────────────────────────
_BQ_PROJECT = os.getenv("BQ_PROJECT", "project-20183357-3fd0-40ef-998")
_BQ_DATASET = os.getenv("BQ_DATASET", "dataflow_platform")


def get_bigquery_client() -> bigquery.Client:
    """Cliente BigQuery usando Application Default Credentials (ADC).
    Configurar con: gcloud auth application-default login
    O setear: GOOGLE_APPLICATION_CREDENTIALS=ruta/al/key.json
    """
    return bigquery.Client(project=_BQ_PROJECT)
