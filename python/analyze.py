"""
analyze.py - Analisis de ventas con pandas.
Entrada : C:\\temp\\ventas_test.csv
Salida  : C:\\temp\\reporte_ventas.csv + reporte en consola
"""

import sys
import io
from pathlib import Path
import pandas as pd

# Fuerza UTF-8 en la consola para evitar UnicodeEncodeError en Windows
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

# -- Rutas --------------------------------------------------------------------
ARCHIVO_CSV = Path(r"C:\temp\ventas_test.csv")
ARCHIVO_REP = Path(r"C:\temp\reporte_ventas.csv")

# -- Helpers de presentacion --------------------------------------------------
ANCHO = 58

def sep(char="-"):
    print(char * ANCHO)

def titulo(texto):
    sep("=")
    print(f"  {texto}")
    sep("=")

def seccion(texto):
    print()
    sep("-")
    print(f"  {texto}")
    sep("-")

def fila(etiqueta, valor, nota=""):
    """Imprime una fila alineada: etiqueta a la izq, valor a la der."""
    print(f"  {etiqueta:<30} {str(valor):>14} {nota}")


# -- 1. Lectura del CSV -------------------------------------------------------
if not ARCHIVO_CSV.exists():
    sys.exit(f"[ERROR] Archivo no encontrado: {ARCHIVO_CSV}")

df = pd.read_csv(ARCHIVO_CSV)

# Normaliza columnas
df.columns = df.columns.str.lower().str.strip()

# Verifica columnas requeridas
cols_req = {"producto", "cantidad", "precio", "region"}
faltantes = cols_req - set(df.columns)
if faltantes:
    sys.exit(f"[ERROR] Columnas faltantes: {faltantes}")

# Limpieza de tipos
df["cantidad"] = pd.to_numeric(df["cantidad"], errors="coerce")
df["precio"]   = pd.to_numeric(df["precio"],   errors="coerce")
df["producto"] = df["producto"].str.strip()
df["region"]   = df["region"].str.strip()

# Solo filas con cantidad y precio validos
df_ok = df.dropna(subset=["cantidad", "precio"]).copy()
df_ok["venta_total"] = df_ok["cantidad"] * df_ok["precio"]


# -- 2. Calcular estadisticas -------------------------------------------------
total_registros   = len(df)
registros_validos = len(df_ok)
registros_nulos   = total_registros - registros_validos
suma_global       = df_ok["venta_total"].sum()
promedio_precio   = df["precio"].mean()
precio_max        = df["precio"].max()
precio_min        = df["precio"].min()

# Producto mas vendido por cantidad
ventas_prod   = df_ok.groupby("producto")["cantidad"].sum()
producto_top  = ventas_prod.idxmax()
cantidad_top  = int(ventas_prod.max())

# Ventas totales por region (mayor a menor)
ventas_region = (
    df_ok.groupby("region")["venta_total"]
    .sum()
    .sort_values(ascending=False)
)
region_top = ventas_region.idxmax()


# -- 3. Reporte en consola ----------------------------------------------------
titulo("REPORTE DE VENTAS - DataFlowPlatform")

seccion("RESUMEN GENERAL")
fila("Total de registros",         total_registros)
fila("Registros validos",          registros_validos)
fila("Registros con datos nulos",  registros_nulos)
fila("Venta total global",         f"${suma_global:,.2f}")

seccion("ESTADISTICAS DE PRECIO")
fila("Precio promedio",            f"${promedio_precio:,.2f}")
fila("Precio maximo",              f"${precio_max:,.2f}")
fila("Precio minimo",              f"${precio_min:,.2f}")

seccion("PRODUCTO MAS VENDIDO")
fila("Producto",                   producto_top)
fila("Unidades vendidas",          cantidad_top)

seccion("VENTAS POR REGION")
for region, total in ventas_region.items():
    nota = "<-- TOP" if region == region_top else ""
    fila(region, f"${total:,.2f}", nota)

seccion("DETALLE POR PRODUCTO (mayor a menor venta)")
print(f"  {'Producto':<20} {'Cant':>5} {'Precio':>10} {'Venta Total':>12}")
sep()
for _, row in df_ok.sort_values("venta_total", ascending=False).iterrows():
    print(
        f"  {row['producto']:<20}"
        f" {int(row['cantidad']):>5}"
        f" {row['precio']:>10.2f}"
        f" {row['venta_total']:>12.2f}"
    )

print()
sep("=")
print(f"  Archivo exportado: {ARCHIVO_REP}")
sep("=")


# -- 4. Exportar resumen a CSV ------------------------------------------------
resumen = df_ok.groupby("region").agg(
    unidades_vendidas = ("cantidad",    "sum"),
    venta_total       = ("venta_total", "sum"),
    precio_promedio   = ("precio",      "mean"),
    num_productos     = ("producto",    "nunique"),
).reset_index()

resumen["venta_total"]     = resumen["venta_total"].round(2)
resumen["precio_promedio"] = resumen["precio_promedio"].round(2)
resumen = resumen.sort_values("venta_total", ascending=False)

# Fila de totales al final
fila_totales = pd.DataFrame([{
    "region":            "TOTAL",
    "unidades_vendidas": int(resumen["unidades_vendidas"].sum()),
    "venta_total":       round(resumen["venta_total"].sum(), 2),
    "precio_promedio":   round(df["precio"].mean(), 2),
    "num_productos":     df_ok["producto"].nunique(),
}])

resumen_final = pd.concat([resumen, fila_totales], ignore_index=True)

# utf-8-sig para que Excel lo abra correctamente en Windows
resumen_final.to_csv(ARCHIVO_REP, index=False, encoding="utf-8-sig")
