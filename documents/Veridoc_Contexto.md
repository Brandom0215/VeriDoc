# Veridoc API

## Contexto general del proyecto y guía de desarrollo

**Documento para:** Antigravity\
**Versión:** 1.0\
**Estado:** Contexto inicial de desarrollo\
**Stack principal:** C#, ASP.NET Core Web API y PostgreSQL

------------------------------------------------------------------------

## 1. Propósito de este documento

Este archivo reúne el contexto funcional y técnico conocido de Veridoc
API para que Antigravity pueda comprender qué se quiere construir,
cuáles son las decisiones tomadas, qué aspectos siguen pendientes y qué
límites debe respetar al proponer o modificar código.

Utiliza este documento como contexto del proyecto. No asumas que una
funcionalidad está implementada solo porque aparece descrita aquí.
Distingue siempre entre:

-   **Decisión definida:** forma parte de la dirección actual del
    proyecto.
-   **Propuesta pendiente:** debe confirmarse antes de implementarse.
-   **Fuera del MVP:** no debe desarrollarse en la primera versión.

Cuando falte información, pregunta o presenta una propuesta explícita
antes de convertirla en una decisión. No inventes reglas de negocio,
campos documentales, endpoints ni requisitos legales.

------------------------------------------------------------------------

## 2. Resumen del proyecto

**Veridoc API** es un servicio backend orientado a validar información
de distintos tipos de documentos mediante reglas programadas.

El sistema recibirá datos estructurados en formato JSON a través de
solicitudes HTTP. Identificará el tipo de documento, ejecutará las
reglas correspondientes y devolverá una respuesta JSON con el resultado
y los errores o inconsistencias detectados.

La solución tendrá una API REST como núcleo y una interfaz gráfica
sencilla, desarrollada por el equipo, únicamente para demostrar el
funcionamiento del servicio: introducir o seleccionar datos de prueba,
enviarlos a la API y visualizar el resultado. Esta interfaz no pretende
ser una plataforma empresarial completa ni sustituye la API como
producto principal.

La primera versión debe ser pequeña, funcional, comprobable y ampliable.
El proyecto se desarrolla en un contexto universitario, con un equipo de
tres integrantes y un plazo aproximado de cuatro semanas.

### Propuesta de valor

Facilitar y automatizar comprobaciones repetitivas sobre documentos,
como verificar campos obligatorios, formatos y consistencia de datos,
para reducir parte del trabajo manual y ayudar a detectar errores antes
de que continúe un proceso administrativo o empresarial.

Veridoc no certifica la autenticidad legal de un documento, no confirma
que la información sea verdadera en el mundo real y no toma decisiones
comerciales o financieras. Solo comprueba las condiciones expresadas en
las reglas implementadas.

------------------------------------------------------------------------

## 3. Problema y antecedentes

En procesos administrativos, empresariales y financieros es frecuente
revisar documentos para comprobar que contienen la información requerida
y que sus datos cumplen determinadas condiciones. La revisión manual
puede consumir tiempo y estar expuesta a errores, especialmente cuando
se maneja un volumen considerable de documentos.

La idea se reforzó mediante una consulta exploratoria a una persona que
participa en la revisión de documentación para trámites de préstamos
bancarios. La persona explicó que comprueba si los documentos de los
clientes cumplen los requisitos del proceso y consideró que una
herramienta de este tipo podría ser útil para organizaciones que manejan
muchas facturas.

Esta conversación es evidencia exploratoria, no una validación
concluyente del mercado. No se ha demostrado todavía cuánto tiempo se
ahorraría, qué soluciones existentes utilizan las empresas ni cuál sería
la demanda comercial.

------------------------------------------------------------------------

## 4. Usuarios y actores

1.  **Empresa o usuario consumidor:** necesita comprobar que ciertos
    documentos cumplen reglas definidas. Envía datos a la API y recibe
    el resultado.
2.  **Sistema cliente / desarrollador integrador:** integra la API desde
    una aplicación web, móvil o de escritorio mediante HTTP y JSON.
3.  **Administrador:** rol previsto para gestionar accesos o
    configuración si esas capacidades se incorporan. Su alcance exacto
    está pendiente.
4.  **Equipo de desarrollo:** tres integrantes responsables del
    análisis, implementación, pruebas, documentación e integración.

La interfaz gráfica propia será una interfaz de demostración del
prototipo. No debe confundirse con una aplicación empresarial completa
ni asumirse que tendrá gestión de usuarios o funciones administrativas
avanzadas.

------------------------------------------------------------------------

## 5. Objetivos

### Objetivo general

Desarrollar, en aproximadamente cuatro semanas, una primera versión
funcional de Veridoc API con C#, ASP.NET Core Web API y PostgreSQL,
capaz de validar al menos dos tipos de documentos mediante reglas
definidas y devolver resultados en JSON.

### Objetivos específicos propuestos

-   Seleccionar dos tipos de documentos y definir al menos cinco reglas
    por cada tipo.
-   Implementar endpoints para recibir datos JSON, ejecutar validaciones
    y devolver resultados.
-   Conectar PostgreSQL para almacenar los registros que finalmente se
    definan como necesarios.
-   Preparar al menos 30 casos de prueba con resultados esperados.
-   Aspirar a un mínimo de 95 % de resultados correctos en el conjunto
    de pruebas definido.
-   Documentar los endpoints con OpenAPI/Swagger.
-   Crear una interfaz de demostración para enviar datos y mostrar los
    resultados.

Los porcentajes y cantidades anteriores son metas de evaluación, no
resultados ya obtenidos.

------------------------------------------------------------------------

## 6. Alcance del MVP

### Incluido

-   API REST desarrollada en C# y ASP.NET Core Web API.
-   Recepción de datos estructurados en JSON mediante HTTP.
-   Soporte inicial para al menos dos tipos de documentos.
-   Reglas de validación específicas por tipo de documento.
-   Comprobación de campos obligatorios, formatos e inconsistencias que
    puedan expresarse mediante reglas.
-   Respuesta JSON clara con el resultado y los errores detectados.
-   Integración con PostgreSQL para los datos que se decida persistir.
-   Manejo controlado de entradas inválidas y errores técnicos.
-   Documentación OpenAPI/Swagger.
-   Pruebas unitarias, de integración y casos funcionales.
-   Interfaz gráfica sencilla para demostrar el flujo de uso de la API.

### Fuera del MVP

-   Agentes de inteligencia artificial o modelos de lenguaje.
-   RAG, MCP u orquestación de agentes.
-   OCR o reconocimiento óptico de caracteres.
-   Lectura o extracción automática de información desde PDF o imágenes.
-   Integraciones con bancos, sistemas contables u otros proveedores
    externos.
-   Aplicación web o móvil empresarial completa.
-   Aprobación de préstamos, decisiones financieras o certificación de
    autenticidad legal.
-   Arquitectura distribuida con varios microservicios, colas de
    mensajes, gRPC o API Gateway.

No agregues estas funciones al MVP salvo que el responsable del proyecto
y el asesor académico cambien expresamente el alcance.

------------------------------------------------------------------------

## 7. Tecnologías y decisiones técnicas actuales

  Área                       Decisión actual
  -------------------------- ----------------------------------------------
  Lenguaje backend           C# (.NET 10)
  Framework backend          ASP.NET Core Web API
  IDE                        Visual Studio
  Base de datos              PostgreSQL (local / docker-compose opcional)
  Comunicación               REST sobre HTTP/HTTPS
  Formato de intercambio     JSON
  Documentación API          OpenAPI/Swagger
  Control de versiones       Git y GitHub
  Interfaz de demostración   Web ligera (HTML + CSS + JS/Fetch)
  Autenticación MVP          Cabecera HTTP `X-Api-Key`
  ORM                        Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
  Pruebas                    xUnit (`Veridoc.Tests`)

### Arquitectura general

La primera versión tendrá un único servicio backend. El módulo de
validación será parte de la misma API, no un microservicio
independiente. PostgreSQL será el almacenamiento de datos, no un
microservicio.

Flujo conceptual:

1.  La interfaz de demostración o un sistema cliente envía una solicitud
    HTTP con datos JSON.
2.  Un endpoint recibe y valida la estructura de entrada.
3.  La API identifica el tipo de documento.
4.  El módulo de validación ejecuta las reglas correspondientes.
5.  Cuando sea necesario, la API consulta o registra datos en
    PostgreSQL.
6.  La API devuelve una respuesta JSON con el resultado o un error
    técnico controlado.
7.  La interfaz de demostración presenta la respuesta al usuario.

### Diagrama textual simplificado

``` text
[Interfaz de demostración / Sistema cliente]
                 |
                 | HTTP/HTTPS + JSON
                 v
          [Veridoc.API]
                 |
        [Endpoints / Controllers]
                 |
       [Validación de entrada]
                 |
       [Módulo de validación]
                 |
       [Reglas por tipo documental]
                 |
          [Acceso a datos]
                 |
            [PostgreSQL]

La API devuelve JSON al cliente.
```

------------------------------------------------------------------------

## 8. Reglas de diseño y organización del código

Mantén responsabilidades separadas. Evita colocar toda la lógica dentro
de `Program.cs` o de los controladores.

Estructura inicial sugerida para la solución:

``` text
Veridoc/
├── Veridoc.API/
│   ├── Controllers/
│   ├── DTOs/
│   ├── Models/
│   ├── Services/
│   ├── Validators/
│   ├── Data/
│   ├── Properties/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Veridoc.API.csproj
├── Veridoc.Tests/          # Proyecto de pruebas, cuando se cree
├── docs/
├── .gitignore
└── Veridoc.sln
```

La estructura puede ajustarse cuando se conozcan las necesidades reales.
No crees capas, proyectos o abstracciones innecesarias solo por seguir
una arquitectura de moda.

### Criterios de implementación

-   Usa nombres claros y consistentes.
-   Mantén los controladores delgados: reciben solicitudes, delegan el
    trabajo y devuelven respuestas HTTP.
-   Coloca la lógica de negocio en servicios o componentes de
    validación.
-   Utiliza DTOs para definir los contratos de entrada y salida de la
    API.
-   Separa los modelos de dominio de los modelos de transporte cuando
    tenga sentido.
-   Centraliza y documenta las reglas de validación.
-   Maneja excepciones y errores de forma controlada.
-   Devuelve códigos HTTP apropiados y respuestas JSON consistentes.
-   Evita duplicar reglas entre controladores y servicios.
-   Escribe código comprensible para estudiantes que deben poder
    explicarlo y mantenerlo.
-   No introduzcas dependencias NuGet sin explicar su propósito y
    necesidad.

------------------------------------------------------------------------

## 9. Contrato conceptual de validación

Todavía no están definidos los nombres finales de endpoints ni el
esquema exacto de cada documento. Como orientación, una operación de
validación podría recibir:

``` json
{
  "documentType": "tipo_por_definir",
  "document": {
    "campoEjemplo": "valor"
  }
}
```

Y devolver una respuesta conceptual como:

``` json
{
  "isValid": false,
  "documentType": "tipo_por_definir",
  "errors": [
    {
      "field": "campoEjemplo",
      "code": "REQUIRED_FIELD",
      "message": "El campo es obligatorio."
    }
  ]
}
```

Estos ejemplos son ilustrativos. No representan todavía un contrato
aprobado. Antes de implementar contratos definitivos, hay que decidir
los dos tipos de documentos, sus campos y las reglas concretas.

### Diferencia importante: resultado inválido vs. error técnico

-   **Documento no válido:** la solicitud tiene una estructura
    procesable, pero los datos no cumplen una o más reglas. La API debe
    devolver el resultado de validación y los errores correspondientes.
-   **Solicitud mal formada:** el JSON o la estructura de entrada no
    puede procesarse correctamente. La API debe devolver un error de
    solicitud apropiado.
-   **Error técnico:** ocurrió un problema interno, de configuración o
    de conexión a PostgreSQL. La API debe devolver un error controlado
    sin exponer detalles sensibles.

No confundas un documento que no cumple las reglas con una caída o fallo
del sistema.

------------------------------------------------------------------------

## 10. Datos y persistencia

PostgreSQL es la base de datos prevista. Sin embargo, el modelo
definitivo todavía debe diseñarse según las reglas y necesidades del
MVP.

Entidades que podrían evaluarse:

-   **TipoDocumento:** identifica las clases de documentos soportadas.
-   **ReglaValidacion:** representa las reglas asociadas a un tipo de
    documento.
-   **Validacion:** representa una ejecución de validación.
-   **DetalleError / ResultadoValidacion:** podría representar los
    errores encontrados, si conviene almacenarlos por separado.

No crees todas estas tablas automáticamente. Primero define si las
reglas serán almacenadas en la base de datos o configuradas en código, y
si se guardará historial de validaciones. El almacenamiento de
documentos completos no está decidido y no debe asumirse.

### Datos sensibles

Los documentos podrían contener nombres, identificaciones, correos,
teléfonos, montos u otra información personal, financiera o comercial.
Aplica minimización de datos: almacena únicamente lo necesario para el
funcionamiento definido.

Utiliza datos ficticios en las pruebas. No guardes documentos completos,
credenciales ni información personal real en logs, ejemplos,
repositorios o capturas.

El contexto legal considerado para Panamá incluye la Ley 81 de 2019
sobre Protección de Datos Personales y su reglamentación. Cualquier
tratamiento real debe revisarse según el uso concreto y las obligaciones
aplicables.

------------------------------------------------------------------------

## 11. Seguridad

La seguridad debe diseñarse desde el inicio, aunque algunas funciones se
implementen después.

-   No subas contraseñas, tokens, cadenas de conexión privadas ni claves
    al repositorio.
-   Mantén secretos fuera del código fuente, usando variables de entorno
    o mecanismos seguros de configuración.
-   No expongas secretos en mensajes de error ni logs.
-   Valida todas las entradas.
-   Usa consultas parametrizadas o un ORM configurado correctamente.
-   Utiliza HTTPS cuando el servicio se exponga fuera del entorno local.
-   Define autenticación y autorización antes de permitir acceso real a
    clientes externos.
-   Aplica el principio de mínimo privilegio.
-   No afirmes que el sistema es seguro sin haber probado sus controles.

La autenticación concreta y el modelo de permisos siguen pendientes de
definición.

------------------------------------------------------------------------

## 12. Interfaz de demostración

El equipo construirá una interfaz propia y sencilla para mostrar el
funcionamiento del proyecto. Su propósito es permitir que un evaluador o
usuario de prueba:

1.  Seleccione el tipo de documento disponible.
2.  Introduzca los datos de ejemplo requeridos.
3.  Envíe la información a la API.
4.  Visualice si el documento cumple las reglas.
5.  Consulte los campos con errores o inconsistencias.

La interfaz debe consumir los endpoints de Veridoc API. No debe duplicar
la lógica de validación: las reglas deben ejecutarse en el backend. La
tecnología de frontend está pendiente de elección.

No se requiere diseñar una plataforma empresarial completa, gestión
avanzada de usuarios ni múltiples aplicaciones para distintas
plataformas.

------------------------------------------------------------------------

## 13. Pruebas y criterios de calidad

Se propone preparar al menos 30 casos de prueba con documentos
ficticios. Deben incluir, como mínimo:

-   Documento válido.
-   Campo obligatorio ausente.
-   Campo con formato incorrecto.
-   Valor fuera de los límites definidos por una regla.
-   Inconsistencia entre campos.
-   Tipo de documento no soportado.
-   JSON mal formado.
-   Error de conexión a la base de datos, cuando la persistencia esté
    implementada.

Meta propuesta: al menos 95 % de resultados correctos en el conjunto de
pruebas definido. Esta cifra debe calcularse con resultados esperados y
obtenidos, no estimarse.

También se planteó como objetivo de rendimiento que el 95 % de las
solicitudes responda en menos de 500 ms bajo condiciones de prueba
definidas. No es un resultado garantizado ni medido todavía.

### Guardarraíles funcionales

-   El sistema solo debe informar errores definidos por reglas
    implementadas.
-   No debe inventar datos ni completar automáticamente información
    ausente.
-   No debe afirmar que un documento es auténtico o legalmente válido.
-   No debe aprobar préstamos ni tomar decisiones financieras.
-   Debe distinguir errores de validación de errores técnicos.
-   Debe devolver mensajes claros sin filtrar información interna.

No hay agentes de IA en el MVP, por lo que no hay evals de agentes,
alucinaciones de modelos ni proveedor de IA que monitorear. Las pruebas
aplicables son las de la API y su motor de reglas.

------------------------------------------------------------------------

## 14. Despliegue, operación y observabilidad

Durante el desarrollo, la aplicación se ejecutará localmente desde
Visual Studio. Se evaluará Docker si facilita que los integrantes
ejecuten el mismo entorno. El despliegue en nube dependerá del tiempo y
los recursos disponibles.

Se propone utilizar GitHub para el control de versiones y evaluar GitHub
Actions para ejecutar compilación y pruebas automáticamente. CI/CD no
debe darse por implementado hasta configurarlo y comprobarlo.

Registros y métricas iniciales sugeridas:

-   Solicitudes recibidas.
-   Cantidad de validaciones realizadas.
-   Errores de validación.
-   Errores técnicos.
-   Tiempos de respuesta.
-   Fallos de conexión con PostgreSQL.

Los logs deben evitar información personal y documental innecesaria.
Puede utilizarse un identificador de correlación por solicitud para
facilitar el diagnóstico.

------------------------------------------------------------------------

## 15. Plan de trabajo propuesto

Equipo: tres integrantes. Plazo aproximado: cuatro semanas.

  -----------------------------------------------------------------------
  Semana                  Actividades             Hito
  ----------------------- ----------------------- -----------------------
  1                       Seleccionar dos tipos   Requisitos y diseño
                          de documentos, definir  inicial definidos.
                          campos y al menos cinco 
                          reglas por tipo,        
                          diseñar modelo de datos 
                          y contratos iniciales.  

  2                       Crear endpoints, DTOs y API recibe JSON y
                          módulo de validación;   valida los primeros
                          implementar primeras    casos.
                          reglas.                 

  3                       Completar reglas,       Flujo integrado de
                          integrar PostgreSQL si  extremo a extremo.
                          aplica, construir       
                          interfaz de             
                          demostración y          
                          documentar endpoints.   

  4                       Ejecutar pruebas,       Prototipo y entregables
                          corregir errores,       listos.
                          revisar seguridad,      
                          completar documentación 
                          y preparar              
                          demostración.           
  -----------------------------------------------------------------------

Distribución inicial sugerida de responsabilidades:

-   Integrante 1: backend, estructura de API y endpoints.
-   Integrante 2: reglas de validación y diseño/integración de
    PostgreSQL.
-   Integrante 3: pruebas, documentación e interfaz de demostración.

Los tres integrantes deben revisar e integrar el trabajo. Los roles
pueden ajustarse según habilidades y disponibilidad.

------------------------------------------------------------------------

## 16. Riesgos principales

  -----------------------------------------------------------------------
  Riesgo                              Mitigación
  ----------------------------------- -----------------------------------
  Retraso en el desarrollo            Priorizar el MVP, dividir tareas y
                                      posponer mejoras opcionales.

  Reglas incorrectas                  Definir reglas con ejemplos y
                                      probar casos válidos e inválidos.

  Problemas con PostgreSQL            Probar la conexión temprano y
                                      manejar fallos de forma controlada.

  Conflictos o pérdida de cambios     Utilizar Git, ramas cuando convenga
                                      y revisar integraciones.

  Exposición de información sensible  Usar datos ficticios, proteger
                                      secretos y minimizar logs.

  Alcance demasiado amplio            No agregar OCR, IA, integraciones
                                      externas ni funciones no aprobadas
                                      al MVP.
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 17. Entregables esperados

-   Código fuente de Veridoc API.
-   Solución de Visual Studio.
-   Base de datos y esquema, según el diseño final.
-   Endpoints documentados mediante OpenAPI/Swagger.
-   Reglas para al menos dos tipos de documentos.
-   Interfaz gráfica sencilla de demostración.
-   Casos de prueba y resultados obtenidos.
-   Diagrama C4 de contexto, contenedores y componentes.
-   Diagrama de flujo de validación.
-   Modelo entidad-relación o de dominio.
-   Registro de decisiones de arquitectura (ADR).
-   Registro de riesgos y cronograma.
-   Documento de visión y documentación técnica.

Los diagramas y documentos deben reflejar el sistema real. No documentes
como implementada una funcionalidad que solo está planificada.

------------------------------------------------------------------------

## 18. Decisiones confirmadas para el diseño del MVP

1.  **Tipos de documentos del MVP:**
    -   `cedula_identidad` (Cédula de Identidad / Datos Personales).
    -   `factura_comercial` (Factura Comercial).
2.  **Campos por tipo de documento:**
    -   **Cédula de Identidad (`cedula_identidad`):** `numeroCedula`, `nombreCompleto`, `fechaNacimiento`, `fechaExpedicion`, `fechaExpiracion`, `nacionalidad`.
    -   **Factura Comercial (`factura_comercial`):** `numeroFactura`, `rucEmisor`, `nombreCliente`, `fechaEmision`, `fechaVencimiento`, `subtotal`, `itbms`, `total`, `moneda`.
3.  **Reglas de validación (6 por tipo):**
    -   **Cédula de Identidad:** (1) Campos obligatorios (`REQUIRED_FIELD`), (2) Formato de cédula panameña (`INVALID_CEDULA_FORMAT`), (3) Fecha de nacimiento válida y no futura (`INVALID_BIRTH_DATE`), (4) Mayoría de edad $\ge 18$ años (`UNDERAGE_HOLDER`), (5) Consistencia `fechaExpiracion > fechaExpedicion > fechaNacimiento` (`INCONSISTENT_ISSUE_EXPIRY_DATES`), (6) Documento vigente `fechaExpiracion >= hoy` (`DOCUMENT_EXPIRED`).
    -   **Factura Comercial:** (1) Campos obligatorios (`REQUIRED_FIELD`), (2) Formato de factura `FAC-XXXX`, RUC y moneda `USD`/`PAB` (`INVALID_FORMAT`), (3) Límites de montos `subtotal > 0`, `itbms >= 0`, `total > 0` (`OUT_OF_RANGE_AMOUNT`), (4) Consistencia aritmética `subtotal + itbms == total` (`INCONSISTENT_TOTAL`), (5) Impuesto congruente al 0% o 7% del subtotal (`INCONSISTENT_TAX_RATE`), (6) Consistencia de fechas `fechaVencimiento >= fechaEmision` y emisión no futura (`INCONSISTENT_INVOICE_DATES`).
4.  **Ubicación de las reglas:** Híbrido: lógica de evaluación en código C# (`Validators/`) y catálogo/metadatos registrados en PostgreSQL (`TipoDocumento`, `ReglaValidacion`).
5.  **Almacenamiento de historial:** Sí, se almacenan los registros de ejecuciones de validación (`Validacion`) y sus errores (`DetalleError`).
6.  **Privacidad y minimización de datos (Ley 81 de 2019):** Solo se almacena el resultado de validación y los códigos/campos de error; **no** se almacena el JSON del documento completo.
7.  **Mecanismo de autenticación del MVP:** API Key enviada en cabecera HTTP (`X-Api-Key`), configurada fuera del código fuente.
8.  **Tecnología de la interfaz de demostración:** Cliente web ligero en HTML + CSS + JavaScript (`fetch`).
9.  **Acceso a datos (ORM):** Entity Framework Core con el proveedor `Npgsql.EntityFrameworkCore.PostgreSQL`.
10. **Entorno de base de datos y pruebas:** PostgreSQL local con `docker-compose.yml` opcional en el repositorio, y proyecto de pruebas `Veridoc.Tests` con **xUnit**.

------------------------------------------------------------------------

## 19. Instrucciones para Antigravity

Actúa como asistente de desarrollo de software y colaborador técnico del
proyecto Veridoc API.

Al trabajar en este repositorio:

1.  Lee este documento y revisa el código existente antes de proponer
    cambios.
2.  Respeta las tecnologías y el alcance descritos.
3.  No reemplaces ni reorganices archivos existentes sin explicar
    primero por qué es necesario.
4.  Antes de implementar una funcionalidad ambigua, identifica qué
    decisión falta y pregunta o presenta opciones.
5.  Divide el trabajo en pasos pequeños y verificables.
6.  Explica qué archivos crearás o modificarás y cuál será su
    responsabilidad.
7.  Escribe código legible, mantenible y apropiado para un proyecto
    universitario que el equipo debe comprender y defender.
8.  No agregues paquetes, servicios, patrones o capas sin justificar su
    utilidad.
9.  Ejecuta o indica cómo ejecutar compilación y pruebas después de
    cambios importantes.
10. No afirmes que una prueba pasó si no se ejecutó.
11. No inventes datos de documentos ni reglas de negocio.
12. No agregues IA, OCR, lectura de PDF, integraciones bancarias ni
    microservicios adicionales al MVP.
13. Mantén la lógica de validación en el backend; la interfaz solo debe
    consumir la API.
14. Protege credenciales y datos sensibles.
15. Actualiza la documentación cuando una decisión se confirme o cambie.

### Forma de trabajo preferida

Para cada tarea:

-   Explica brevemente el objetivo.
-   Indica los archivos que se crearán o modificarán.
-   Presenta la implementación por partes.
-   Explica cómo ejecutarla y probarla.
-   Señala errores, limitaciones y decisiones pendientes.
-   Espera confirmación antes de avanzar a cambios grandes o
    irreversibles.

**Prioridad principal:** construir primero un MVP funcional,
comprensible y verificable. La simplicidad bien diseñada es preferible a
una arquitectura innecesariamente compleja.
