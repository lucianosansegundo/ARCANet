# Configuración de Homologación

Guia breve para preparar credenciales de homologacion ARCA/AFIP y usarlas con los integration tests opt-in de `ARCANet`.

Importante:

- `ARCANet` no reemplaza validacion contable/fiscal profesional.
- No commitees certificados, claves privadas, passwords, tokens ni CUIT de terceros.
- Los integration tests actuales de homologacion son smoke tests de lectura controlada.
- La suite actual no emite comprobantes nuevos automaticamente.

## 1. Requisitos previos

Necesitas:

- CUIT con clave fiscal.
- Acceso al servicio `WSASS` para homologacion/testing.
- Un punto de venta habilitado para comprobantes electronicos por `Web Services`.
- OpenSSL o herramienta equivalente para generar clave privada y CSR.
- Un certificado de homologacion asociado al servicio `wsfe`.

Relacion con el modo del POS:

- si tu POS ofrece un "modo test", en `ARCANet` eso deberia mapearse a `ArcaEnvironment.Homologation`
- el modo real/productivo deberia mapearse a `ArcaEnvironment.Production`
- no conviene tratar ambos como el mismo entorno con solo distintas credenciales

Motivo:

- homologacion y produccion usan endpoints distintos
- usan certificados distintos
- usan relaciones/habilitaciones administrativas distintas
- el cache de `Access Ticket` tambien debe quedar separado por ambiente

Fuentes oficiales:

- [WSAA - documentacion](https://www.afip.gob.ar/ws/documentacion/wsaa.asp)
- [Certificados - documentacion](https://www.afip.gob.ar/ws/documentacion/certificados.asp)
- [WSASS - manual del usuario](https://www.afip.gob.ar/ws/WSASS/html/index.html)
- [WS factura electronica - documentacion](https://www.afip.gob.ar/ws/documentacion/ws-factura-electronica.asp)
- [WSFEv1 - manual para desarrollador](https://www.afip.gob.ar/fe/ayuda/documentos/wsfev1-RG-4291.pdf)
- [Acciones para consumir un Web Service de Factura Electronica](https://www.afip.gob.ar/fe/documentos/AccionesarealizarparaconsumirunWebservicedeFacturaElectr.pdf)

## 2. Generar clave privada y CSR

Segun el manual de `WSASS`, primero genera una clave privada RSA de al menos 2048 bits:

```powershell
openssl genrsa -out MiClavePrivada 2048
```

Luego genera el `CSR` en formato `PKCS#10`. El `serialNumber` debe contener `CUIT ` seguido de los 11 digitos, sin guiones:

```powershell
openssl req `
  -new `
  -key MiClavePrivada `
  -subj "/C=AR/O=EmpresaPrueba/CN=ARCANetHomologacion/serialNumber=CUIT 20123456789" `
  -out MiPedidoCSR
```

Puntos a respetar:

- `O` puede ser tu empresa o nombre organizacional.
- `CN` puede ser el nombre del sistema cliente.
- `serialNumber` debe seguir exactamente el formato publicado por `WSASS`.
- Guarda la clave privada en un lugar seguro.

Fuente oficial:

- [WSASS - como generar una solicitud de certificado (CSR)](https://www.afip.gob.ar/ws/WSASS/html/generarcsr.html)

## 3. Obtener el certificado de homologacion en WSASS

Para homologacion, ARCA publica que el certificado digital de testing se gestiona con `WSASS`.

Pasos:

1. Ingresa con clave fiscal al portal de ARCA/AFIP.
2. Accede a `WSASS`.
3. Si es tu primer certificado para ese DN, usa `Nuevo Certificado`.
4. Carga:
   - alias o nombre simbolico del DN
   - tu `CSR` en formato `PKCS#10`
5. Descarga o copia el certificado emitido en formato `PEM`.

Notas oficiales relevantes:

- `WSASS` es para testing/homologacion, no para produccion.
- El `CUIT` del formulario debe coincidir con el incluido en el `CSR`.
- El DN queda con formato `SERIALNUMBER=CUIT nnnnnnnnnnn, CN=xxxxx`.

Fuentes oficiales:

- [WSAA - documentacion](https://www.afip.gob.ar/ws/documentacion/wsaa.asp)
- [WSASS - conceptos basicos](https://www.afip.gob.ar/ws/WSASS/html/conceptos.html)
- [WSASS - como crear un certificado nuevo](https://www.afip.gob.ar/ws/WSASS/html/crearcertificado.html)

## 4. Crear el archivo PFX

`ARCANet` usa un `PFX/PKCS#12` local para los integration tests actuales.

Si `WSASS` te devolvio un certificado `PEM`, combina ese certificado con la clave privada original:

```powershell
openssl pkcs12 `
  -export `
  -inkey MiClavePrivada `
  -in certificado.pem `
  -out certificado.pfx
```

Guarda el `.pfx` fuera del repo.

Fuente oficial:

- [WSASS - como crear un certificado nuevo](https://www.afip.gob.ar/ws/WSASS/html/crearcertificado.html)

## 5. Autorizar el servicio wsfe en homologacion

Ademas del certificado, necesitas autorizar acceso al servicio de negocio correspondiente.

Para el alcance actual de `ARCANet`, el servicio es:

- `wsfe`

Pasos orientativos:

1. En `WSASS`, usa `Crear Autorizacion a Servicio`.
2. Selecciona el servicio correspondiente a `WSFE`.
3. Informa la `CUIT representada` si corresponde.
4. Confirma la autorizacion.

El material oficial tambien aclara que no hace falta un certificado distinto por cada web service.

Fuentes oficiales:

- [WSASS - conceptos basicos](https://www.afip.gob.ar/ws/WSASS/html/conceptos.html)
- [Acciones para consumir un Web Service de Factura Electronica](https://www.afip.gob.ar/fe/documentos/AccionesarealizarparaconsumirunWebservicedeFacturaElectr.pdf)

## 6. Habilitar un punto de venta para Web Services

Antes de consultar o emitir, necesitas un punto de venta habilitado.

Pasos administrativos:

1. Ingresa con clave fiscal al servicio `Administracion de Puntos de Venta y Domicilios`.
2. Entra a `A/B/M de puntos de venta` o la opcion equivalente vigente.
3. Crea un punto de venta para comprobantes electronicos.
4. Asocia el punto de venta al canal `Web Services`.
5. Vinculalo al domicilio correspondiente.

Reglas importantes publicadas por ARCA:

- Los puntos de venta para `Web Services` deben habilitarse previamente.
- Los puntos de venta de `Web Services`, `Comprobantes en linea` y otros canales no deben mezclarse entre si.

Fuentes oficiales:

- [Factura electronica - micrositio](https://www.afip.gob.ar/fe/)
- [Guia paso a paso - Administracion de puntos de venta](https://serviciosweb.afip.gob.ar/genericos/guiaspasopaso/VerGuia.aspx?id=281)
- [Habilitacion de puntos de venta](https://www.afip.gob.ar/derechos-de-exportacion-de-servicios/comprobantes-y-facturacion/puntos-de-venta.asp)
- [ABC / consultas - puntos de venta distintos por canal](https://servicioscf.afip.gob.ar/publico/abc/consultas_detalle.aspx?id=12012763)

## 7. Configurar variables de entorno para los integration tests

Los tests de homologacion de `ARCANet` estan desactivados por defecto. Se habilitan con estas variables:

Requeridas:

- `ARCANET_RUN_HOMOLOGATION_TESTS=true`
- `ARCANET_TEST_CUIT`
- `ARCANET_TEST_CERTIFICATE_PATH`
- `ARCANET_TEST_CERTIFICATE_PASSWORD`
- `ARCANET_TEST_POINT_OF_SALE`

Adicional para tests que emiten comprobantes nuevos:

- `ARCANET_RUN_HOMOLOGATION_ISSUANCE_TESTS=true`

Opcional recomendado para reusar el mismo `TA` entre corridas:

- `ARCANET_TEST_ACCESS_TICKET_STORE_PATH`
  - si no se informa, el default actual es una carpeta bajo `%TEMP%\ARCANet\HomologationAccessTickets`

Opcionales:

- `ARCANET_TEST_VOUCHER_TYPE`
  - default actual: `6`
- `ARCANET_TEST_VOUCHER_TYPE_NAME`
  - default actual: `Factura B`
- `ARCANET_TEST_EXISTING_VOUCHER_NUMBER`
  - necesario solo para el test de `GetInvoiceAsync`
- `ARCANET_TEST_HTTP_TIMEOUT_SECONDS`
  - default actual: `45`

Ejemplo en PowerShell:

```powershell
$env:ARCANET_RUN_HOMOLOGATION_TESTS = "true"
$env:ARCANET_RUN_HOMOLOGATION_ISSUANCE_TESTS = "true"
$env:ARCANET_TEST_CUIT = "20123456789"
$env:ARCANET_TEST_CERTIFICATE_PATH = "C:\secrets\arca-homo.pfx"
$env:ARCANET_TEST_CERTIFICATE_PASSWORD = "tu-password-local"
$env:ARCANET_TEST_POINT_OF_SALE = "5"
$env:ARCANET_TEST_ACCESS_TICKET_STORE_PATH = "C:\tmp\arcanet-homo-access-tickets"
$env:ARCANET_TEST_VOUCHER_TYPE = "6"
$env:ARCANET_TEST_VOUCHER_TYPE_NAME = "Factura B"
$env:ARCANET_TEST_EXISTING_VOUCHER_NUMBER = "1234"
$env:ARCANET_TEST_HTTP_TIMEOUT_SECONDS = "45"
```

## 8. Ejecutar los integration tests

Para correr solo la suite de homologacion:

```powershell
dotnet test --filter "Category=Integration"
```

Comportamiento actual:

- Si falta configuracion, los tests quedan `skipped`.
- La suite smoke actual cubre:
  - obtencion de access ticket WSAA para `wsfe`
  - `GetLastAuthorizedNumberAsync`
  - `GetInvoiceAsync` sobre un comprobante ya existente
- Los tests de homologacion usan un `FileAccessTicketStore` durable para poder recuperar un `TA` valido entre corridas separadas.
- La suite de emision real queda desactivada salvo que tambien se configure `ARCANET_RUN_HOMOLOGATION_ISSUANCE_TESTS=true`.
- Los casos de emision real automatizados actuales son:
  - `Factura B`
  - `Factura A`
  - `Nota de Credito B`
  - `Nota de Credito A`

## 9. Que verificar si algo falla

Checklist rapido:

- El certificado fue emitido por `WSASS` y es de homologacion.
- El `PFX` contiene el certificado y la clave privada correcta.
- La `CUIT` del entorno coincide con la del `CSR` y con la representacion autorizada.
- El servicio `wsfe` quedo autorizado.
- El punto de venta esta habilitado para `Web Services`.
- El numero de comprobante configurado en `ARCANET_TEST_EXISTING_VOUCHER_NUMBER` realmente existe para ese `PtoVta + Tipo`.
- El reloj local no esta desfasado de forma relevante.

Fuentes oficiales para troubleshooting:

- [WSAA - especificacion tecnica](https://www.afip.gob.ar/ws/WSAA/Especificacion_Tecnica_WSAA_1.2.2.pdf)
- [WSAA - manual del desarrollador](https://www.afip.gob.ar/ws/WSAA/WSAAmanualDev.pdf)
- [WSFEv1 - manual para desarrollador](https://www.afip.gob.ar/fe/ayuda/documentos/wsfev1-RG-4291.pdf)

Caso observado util:

- Si `WSAA` responde un fault como `coe.alreadyAuthenticated` con un mensaje del estilo `El CEE ya posee un TA valido para el acceso al WSN solicitado`, eso suele indicar que ese mismo certificado/CEE ya tiene un `TA` vigente para `wsfe`.
- En ese caso, no conviene asumir de inmediato que el certificado o la autorizacion estan mal.
- Puede significar simplemente que:
  - ya hubo un login exitoso reciente con ese certificado
  - otra app, script o corrida previa ya abrio un `TA`
  - hay que esperar a que ese `TA` expire antes de volver a pedir otro
- Para smoke tests repetidos, conviene reutilizar el mismo cache/proveedor de access ticket dentro de la misma corrida.
- Los tests de homologacion de este repo ya usan `FileAccessTicketStore`, asi que pueden recuperar un `TA` valido de una corrida anterior si el store path sigue siendo el mismo.
- Si queres resetear manualmente ese estado local, borra el directorio configurado en `ARCANET_TEST_ACCESS_TICKET_STORE_PATH` o deja que expire el `TA`.
