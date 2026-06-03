# Samples

Las samples del repo no forman parte de los paquetes publicados. Sirven para validacion manual y ejemplos ejecutables.

## Rendering sample

La sample `ARCANet.Rendering.Sample` genera archivos `HTML` o `PDF` de comprobantes fiscales de ejemplo para inspeccion visual.

Criterio recomendado:

- `A4`: `PDF` como salida principal
- `thermal58` y `thermal80`: `HTML` como salida principal para impresion
- si no se informa `--format`, la sample usa ese default automaticamente

Ejemplos:

```powershell
dotnet run --project samples/ARCANet.Rendering.Sample -- --layout a4 --scenario short-factura-a --output C:\tmp\factura-a.pdf
dotnet run --project samples/ARCANet.Rendering.Sample -- --layout thermal58 --scenario long-factura-b --output C:\tmp\ticket-58.html
dotnet run --project samples/ARCANet.Rendering.Sample -- --layout thermal80 --scenario credit-note-b --output C:\tmp\nc-80.html
```

Parametros:

- `--format`: `html` o `pdf` (opcional)
- `--layout`: `a4`, `thermal58`, `thermal80`
- `--scenario`: `short-factura-a`, `long-factura-b`, `credit-note-b`
- `--output`: ruta opcional de salida
