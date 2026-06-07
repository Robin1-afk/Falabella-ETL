"""
Migra la tabla `sales` de SQL Server → BigQuery (dataflow_platform.sales).
Ejecutar: py seed_bigquery.py
"""
import sys
from database import query_df, get_bigquery_client, _BQ_PROJECT, _BQ_DATASET
from google.cloud import bigquery

TABLE    = "sales"
FULL_REF = f"{_BQ_PROJECT}.{_BQ_DATASET}.{TABLE}"

SCHEMA = [
    bigquery.SchemaField("fecha",     "DATE",    mode="REQUIRED"),
    bigquery.SchemaField("pais",      "STRING",  mode="REQUIRED"),
    bigquery.SchemaField("ciudad",    "STRING",  mode="REQUIRED"),
    bigquery.SchemaField("sede",      "STRING",  mode="REQUIRED"),
    bigquery.SchemaField("producto",  "STRING",  mode="REQUIRED"),
    bigquery.SchemaField("categoria", "STRING",  mode="REQUIRED"),
    bigquery.SchemaField("monto",     "FLOAT64", mode="REQUIRED"),
    bigquery.SchemaField("cantidad",  "INTEGER", mode="REQUIRED"),
    bigquery.SchemaField("estado",    "STRING",  mode="REQUIRED"),
]


def main():
    client = get_bigquery_client()

    # Dataset
    dataset_ref = bigquery.Dataset(f"{_BQ_PROJECT}.{_BQ_DATASET}")
    dataset_ref.location = "US"
    client.create_dataset(dataset_ref, exists_ok=True)
    print(f"Dataset '{_BQ_DATASET}' listo.")

    # Tabla
    table_ref = bigquery.Table(FULL_REF, schema=SCHEMA)
    client.create_table(table_ref, exists_ok=True)
    print(f"Tabla '{TABLE}' lista.")

    # Leer de SQL Server
    df = query_df(
        "SELECT fecha, pais, ciudad, sede, producto, categoria, monto, cantidad, estado "
        "FROM sales ORDER BY fecha"
    )
    print(f"{len(df)} filas leídas de SQL Server.")

    if df.empty:
        print("No hay datos para migrar.")
        return

    # Preparar tipos para insert_rows_json
    df["fecha"]    = df["fecha"].astype(str)       # datetime.date → "YYYY-MM-DD"
    df["monto"]    = df["monto"].astype(float)
    df["cantidad"] = df["cantidad"].astype(int)

    rows = df.to_dict(orient="records")

    # Vaciar tabla antes de insertar (evita duplicados en re-ejecuciones)
    client.query(f"DELETE FROM `{FULL_REF}` WHERE TRUE").result()
    print("Tabla vaciada (re-seed limpio).")

    errors = client.insert_rows_json(FULL_REF, rows)
    if errors:
        print(f"Errores al insertar: {errors}")
        sys.exit(1)
    else:
        print(f"{len(rows)} filas insertadas en BigQuery exitosamente.")


if __name__ == "__main__":
    main()
