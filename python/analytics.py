import pandas as pd
from google.cloud import bigquery
from database import get_bigquery_client, _BQ_PROJECT, _BQ_DATASET

_TABLE = f"`{_BQ_PROJECT}.{_BQ_DATASET}.sales`"


# ── Helpers ─────────────────────────────────────────────────────────────────────

def _build_bq_where(
    pais: str | None,
    sede: str | None,
    categoria: str | None,
    fecha_inicio: str | None,
    fecha_fin: str | None,
) -> tuple[str, list]:
    conditions: list[str] = []
    params: list[bigquery.ScalarQueryParameter] = []

    if pais:
        conditions.append("pais = @pais")
        params.append(bigquery.ScalarQueryParameter("pais", "STRING", pais))
    if sede:
        conditions.append("sede = @sede")
        params.append(bigquery.ScalarQueryParameter("sede", "STRING", sede))
    if categoria:
        conditions.append("categoria = @categoria")
        params.append(bigquery.ScalarQueryParameter("categoria", "STRING", categoria))
    if fecha_inicio:
        conditions.append("fecha >= @fecha_inicio")
        params.append(bigquery.ScalarQueryParameter("fecha_inicio", "DATE", fecha_inicio))
    if fecha_fin:
        conditions.append("fecha <= @fecha_fin")
        params.append(bigquery.ScalarQueryParameter("fecha_fin", "DATE", fecha_fin))

    where = "WHERE " + " AND ".join(conditions) if conditions else ""
    return where, params


def _bq_query(sql: str, params: list | None = None) -> pd.DataFrame:
    client = get_bigquery_client()
    job_config = bigquery.QueryJobConfig(query_parameters=params) if params else None
    return client.query(sql, job_config=job_config).to_dataframe()


# ── Funciones ───────────────────────────────────────────────────────────────────

def get_filters() -> dict:
    df = _bq_query(f"SELECT DISTINCT pais, sede, categoria FROM {_TABLE}")
    return {
        "paises":     sorted(df["pais"].dropna().unique().tolist()),
        "sedes":      sorted(df["sede"].dropna().unique().tolist()),
        "categorias": sorted(df["categoria"].dropna().unique().tolist()),
    }


def get_filters_sedes(pais: str | None) -> list[str]:
    where, params = _build_bq_where(pais, None, None, None, None)
    df = _bq_query(
        f"SELECT DISTINCT sede FROM {_TABLE} {where} ORDER BY sede",
        params or None,
    )
    return df["sede"].tolist()


def get_summary(
    pais: str | None,
    sede: str | None,
    categoria: str | None,
    fecha_inicio: str | None,
    fecha_fin: str | None,
) -> dict:
    where, params = _build_bq_where(pais, sede, categoria, fecha_inicio, fecha_fin)
    df = _bq_query(f"SELECT monto, estado FROM {_TABLE} {where}", params or None)

    if df.empty:
        return {"totalMonto": 0, "totalTransacciones": 0, "promedio": 0, "tasaExito": 0.0}

    transacciones = len(df)
    return {
        "totalMonto":         round(float(df["monto"].sum()), 2),
        "totalTransacciones": transacciones,
        "promedio":           round(float(df["monto"].mean()), 2),
        "tasaExito":          round(100.0 * (df["estado"] == "Completado").sum() / transacciones, 1),
    }


def get_by_sede(
    pais: str | None,
    categoria: str | None,
    fecha_inicio: str | None,
    fecha_fin: str | None,
) -> list[dict]:
    where, params = _build_bq_where(pais, None, categoria, fecha_inicio, fecha_fin)
    df = _bq_query(f"SELECT sede, monto FROM {_TABLE} {where}", params or None)

    if df.empty:
        return []

    g = df.groupby("sede")["monto"].agg(monto="sum", transacciones="count").reset_index()
    g["monto"] = g["monto"].round(2)
    return g.sort_values("monto", ascending=False).to_dict(orient="records")


def get_by_month(
    pais: str | None,
    sede: str | None,
    categoria: str | None,
    fecha_inicio: str | None,
    fecha_fin: str | None,
) -> list[dict]:
    where, params = _build_bq_where(pais, sede, categoria, fecha_inicio, fecha_fin)
    df = _bq_query(f"SELECT fecha, monto FROM {_TABLE} {where}", params or None)

    if df.empty:
        return []

    df["fecha"] = pd.to_datetime(df["fecha"])
    df["mes"]   = df["fecha"].dt.to_period("M").astype(str)
    g = df.groupby("mes")["monto"].agg(monto="sum", transacciones="count").reset_index()
    g["monto"] = g["monto"].round(2)
    return g.sort_values("mes").to_dict(orient="records")


def get_by_category(
    pais: str | None,
    sede: str | None,
    fecha_inicio: str | None,
    fecha_fin: str | None,
) -> list[dict]:
    where, params = _build_bq_where(pais, sede, None, fecha_inicio, fecha_fin)
    df = _bq_query(f"SELECT categoria, monto FROM {_TABLE} {where}", params or None)

    if df.empty:
        return []

    g = df.groupby("categoria")["monto"].agg(monto="sum", transacciones="count").reset_index()
    g["monto"] = g["monto"].round(2)
    return g.sort_values("monto", ascending=False).to_dict(orient="records")
