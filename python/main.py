from fastapi import FastAPI, Query
from fastapi.middleware.cors import CORSMiddleware
from typing import Optional
import analytics

app = FastAPI(title="Falabella Analytics API", version="1.0.0")

# CORS para Angular en localhost:4200
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:4200"],
    allow_methods=["GET"],
    allow_headers=["*"],
)


# ── GET /analytics/filters ─────────────────────────────────────────────────────
@app.get("/analytics/filters")
def filters():
    """Valores distintos para poblar los dropdowns."""
    return analytics.get_filters()


# ── GET /analytics/filters/sedes ──────────────────────────────────────────────
@app.get("/analytics/filters/sedes")
def filters_sedes(pais: Optional[str] = Query(None)):
    """Sedes disponibles, filtradas opcionalmente por país."""
    return analytics.get_filters_sedes(pais)


# ── GET /analytics/summary ─────────────────────────────────────────────────────
@app.get("/analytics/summary")
def summary(
    pais:         Optional[str] = Query(None),
    sede:         Optional[str] = Query(None),
    categoria:    Optional[str] = Query(None),
    fechaInicio:  Optional[str] = Query(None, alias="fechaInicio"),
    fechaFin:     Optional[str] = Query(None, alias="fechaFin"),
):
    """KPIs: monto total, transacciones, promedio, tasa de éxito."""
    return analytics.get_summary(pais, sede, categoria, fechaInicio, fechaFin)


# ── GET /analytics/by-sede ─────────────────────────────────────────────────────
@app.get("/analytics/by-sede")
def by_sede(
    pais:        Optional[str] = Query(None),
    categoria:   Optional[str] = Query(None),
    fechaInicio: Optional[str] = Query(None, alias="fechaInicio"),
    fechaFin:    Optional[str] = Query(None, alias="fechaFin"),
):
    """Monto y transacciones agrupados por sede."""
    return analytics.get_by_sede(pais, categoria, fechaInicio, fechaFin)


# ── GET /analytics/by-month ────────────────────────────────────────────────────
@app.get("/analytics/by-month")
def by_month(
    pais:        Optional[str] = Query(None),
    sede:        Optional[str] = Query(None),
    categoria:   Optional[str] = Query(None),
    fechaInicio: Optional[str] = Query(None, alias="fechaInicio"),
    fechaFin:    Optional[str] = Query(None, alias="fechaFin"),
):
    """Evolución mensual de ventas."""
    return analytics.get_by_month(pais, sede, categoria, fechaInicio, fechaFin)


# ── GET /analytics/by-category ─────────────────────────────────────────────────
@app.get("/analytics/by-category")
def by_category(
    pais:        Optional[str] = Query(None),
    sede:        Optional[str] = Query(None),
    fechaInicio: Optional[str] = Query(None, alias="fechaInicio"),
    fechaFin:    Optional[str] = Query(None, alias="fechaFin"),
):
    """Distribución de ventas por categoría."""
    return analytics.get_by_category(pais, sede, fechaInicio, fechaFin)
