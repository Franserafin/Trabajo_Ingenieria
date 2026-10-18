# Documentación Técnica: Objeto de Dominio `Coupon` (Cupón de Descuento)

## 1. Descripción del Objeto de Dominio Elegido

Para ampliar el sistema de comercio electrónico sin duplicar funcionalidades existentes (productos, carritos, usuarios invitados y órdenes), se diseñó e implementó el agregado **`Coupon` (Cupón de Descuento)**.

En un e-commerce, los cupones de descuento representan un elemento clave en el flujo de compras:
- Permiten aplicar descuentos promocionales porcentuales a órdenes o carritos.
- Poseen ciclo de vida propio, estados, condiciones de elegibilidad y restricciones de uso.
- Encapsulan lógica de negocio rica, evitando modelos anémicos y respetando **Clean Architecture** y **Domain-Driven Design (DDD)**.

---

## 2. Reglas de Negocio del Dominio

El agregado `Coupon` vive en `ISII.Web.Domain.CouponAggregate` y hereda de `EntityBase<Coupon, CouponId>` e implementa `IAggregateRoot`.

### Propiedades y Value Objects
| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `CouponId` | Value Object fuertemente tipado (generado con `Vogen`, no negativo). |
| `Code` | `string` | Código alfanumérico identificador único en mayúsculas (ej. `WELCOME10`, `PROMO25`). |
| `DiscountPercentage` | `decimal` | Porcentaje de descuento (entre `0.01` y `100.00`). |
| `MinimumAmount` | `decimal` | Monto mínimo de compra requerido para aplicar el cupón (mayor o igual a 0). |
| `ExpirationDate` | `DateTimeOffset` | Fecha y hora límite de validez. Al crearse, debe estar en el futuro. |
| `MaxUsages` | `int` | Cantidad máxima de veces que el cupón puede ser utilizado (mayor a 0). |
| `TimesUsed` | `int` | Contador de usos consumidos (inicia en 0). |
| `IsActive` | `bool` | Estado activo/inactivo del cupón. |

### Reglas e Invariantes del Dominio
1. **Normalización de Código**: El código se almacena sin espacios y en mayúsculas (`Trim().ToUpperInvariant()`).
2. **Protección de Invariantes en Constructor y Factory**:
   - `Code` no puede ser nulo, vacío ni contener sólo espacios en blanco (`Guard.Against.NullOrWhiteSpace`).
   - `DiscountPercentage` debe ser estrictamente mayor a 0 y menor o igual a 100 (`ArgumentOutOfRangeException`).
   - `MinimumAmount` no puede ser negativo (`Guard.Against.Negative`).
   - `MaxUsages` debe ser mayor a 0.
3. **Elegibilidad y Verificación (`CanBeAppliedTo(orderAmount, now)`)**:
   - Retorna `false` si el cupón está inactivo (`!IsActive`).
   - Retorna `false` si el cupón está vencido (`now > ExpirationDate`).
   - Retorna `false` si el monto de la compra es menor al monto mínimo (`orderAmount < MinimumAmount`).
   - Retorna `false` si ya se alcanzó el límite de usos (`TimesUsed >= MaxUsages`).
4. **Cálculo de Descuento (`CalculateDiscount(orderAmount, now)`)**:
   - Valida primero si el cupón puede ser aplicado con `CanBeAppliedTo`. Si no es aplicable, arroja `InvalidOperationException`.
   - Calcula el descuento exacto: `Math.Round(orderAmount * (DiscountPercentage / 100m), 2)`.
5. **Consumo y Desactivación Automática (`RecordUsage(orderAmount, now)`)**:
   - Verifica la validez con `CanBeAppliedTo`.
   - Incrementa `TimesUsed`.
   - Si `TimesUsed >= MaxUsages`, transiciona automáticamente el cupón a inactivo (`IsActive = false`).
6. **Gestión de Estados (`Activate()`, `Deactivate()`)**:
   - No permite activar un cupón vencido (`InvalidOperationException`).
7. **Modificación de Vencimiento (`UpdateExpirationDate(newDate, now)`)**:
   - Requiere que la nueva fecha sea estrictamente posterior al momento actual.

---

## 3. Persistencia y Cambios en la Base de Datos

1. **Configuración EF Core (`CouponConfiguration.cs`)**:
   - Clave primaria `Id` mapeada a la tabla `Coupons` usando el value generator `VogenIntIdValueGenerator<AppDbContext, Coupon, CouponId>()` y `HasSentinel(CouponId.New)`.
   - Conversión Vogen registrada en `VogenEfCoreConverters.cs` con `[EfCoreConverter<CouponId>]`.
   - Índice único en columna `Code` (`builder.HasIndex(e => e.Code).IsUnique()`).
   - Precisiones decimales configuradas para SQLite y SQL Server (`DiscountPercentage`: precision 5, scale 2; `MinimumAmount`: precision 18, scale 2).
   - Sembrado inicial (`HasData`) con el cupón `WELCOME10` (10% de descuento, compra mínima $20, válido hasta 2030, 100 usos).
2. **Contexto de Base de Datos (`AppDbContext.cs`)**:
   - Se incorporó `public DbSet<Coupon> Coupons => Set<Coupon>();`.
3. **Migración EF Core (`20261018202929_AddCoupon.cs`)**:
   - Generada mediante `dotnet ef migrations add AddCoupon`.
   - Aplicada a la base de datos SQLite mediante `dotnet ef database update`.

---

## 4. Endpoints y Vertical Slice (`CouponFeatures`)

El proyecto sigue **Vertical Slice Architecture** con **FastEndpoints** y **CQRS (Mediator)**:

### 4.1. Endpoint para Crear Cupón: `POST /coupons`
- **Ruta**: `POST /coupons`
- **Ubicación**: `src/ISII.Web/CouponFeatures/Create/CreateCouponEndpoint.cs`
- **Comportamiento**:
  1. Recibe `CreateCouponRequest`.
  2. Valida los datos con `CreateCouponValidator` (FluentValidation). Si falla, retorna `400 Bad Request` con el detalle de los errores de validación.
  3. Verifica mediante `CouponByCodeSpec` si ya existe un cupón con ese código. Si existe, retorna `409 Conflict`.
  4. Crea la entidad de dominio con `Coupon.Create(...)`.
  5. Persiste el cupón con `IRepository<Coupon>.AddAsync` y `SaveChangesAsync`.
  6. Retorna `201 Created` con el header `Location: /coupons/{id}` y el cuerpo `CouponRecord`.

#### Ejemplo de Solicitud (Crear Cupón)
```http
POST /coupons HTTP/1.1
Host: localhost:57379
Content-Type: application/json

{
  "code": "PROMO25",
  "discountPercentage": 25.0,
  "minimumAmount": 50.00,
  "expirationDate": "2030-12-31T23:59:59Z",
  "maxUsages": 100
}
```

#### Ejemplo de Respuesta Exitosa (`201 Created`)
```http
HTTP/1.1 201 Created
Location: /coupons/2
Content-Type: application/json

{
  "id": 2,
  "code": "PROMO25",
  "discountPercentage": 25.0,
  "minimumAmount": 50.0,
  "expirationDate": "2030-12-31T23:59:59+00:00",
  "maxUsages": 100,
  "timesUsed": 0,
  "isActive": true
}
```

#### Ejemplo de Error de Validación (`400 Bad Request`)
```http
POST /coupons HTTP/1.1
Host: localhost:57379
Content-Type: application/json

{
  "code": "!",
  "discountPercentage": 150.0,
  "minimumAmount": -10.00,
  "expirationDate": "2020-01-01T00:00:00Z",
  "maxUsages": 0
}
```
Respuesta:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Code": [
      "Coupon code must be at least 3 characters",
      "Coupon code must contain only letters, numbers, hyphens, or underscores"
    ],
    "DiscountPercentage": [
      "Discount percentage cannot exceed 100"
    ],
    "MinimumAmount": [
      "Minimum amount cannot be negative"
    ],
    "ExpirationDate": [
      "Expiration date must be in the future"
    ],
    "MaxUsages": [
      "Max usages must be greater than zero"
    ]
  }
}
```

---

### 4.2. Endpoint para Consultar Cupón por Identificador: `GET /coupons/{CouponId}`
- **Ruta**: `GET /coupons/{CouponId}`
- **Ubicación**: `src/ISII.Web/CouponFeatures/GetById/GetCouponByIdEndpoint.cs`
- **Patrón CQRS**: Despacha `GetCouponQuery(CouponId)` al manejador `GetCouponHandler`.
- **Comportamiento**:
  1. Valida con `GetCouponByIdValidator` que `CouponId > 0`.
  2. El Handler ejecuta la consulta usando `IReadRepository<Coupon>` y la especificación `CouponByIdSpec`.
  3. Si la entidad no existe, retorna `404 Not Found`.
  4. Si existe, mapea a `CouponDto`, y el endpoint retorna `200 OK` con `CouponRecord`.

#### Ejemplo de Solicitud (Consultar Cupón por ID)
```http
GET /coupons/1 HTTP/1.1
Host: localhost:57379
Accept: application/json
```

#### Ejemplo de Respuesta Exitosa (`200 OK`)
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 1,
  "code": "WELCOME10",
  "discountPercentage": 10.0,
  "minimumAmount": 20.0,
  "expirationDate": "2030-12-31T23:59:59+00:00",
  "maxUsages": 100,
  "timesUsed": 0,
  "isActive": true
}
```

#### Ejemplo de Respuesta No Encontrado (`404 Not Found`)
```http
GET /coupons/9999 HTTP/1.1
Host: localhost:57379
Accept: application/json
```
Respuesta: `HTTP/1.1 404 Not Found`

---

## 5. Pruebas Automatizadas

Se implementaron **52 pruebas unitarias e integrales** nuevas (elevando la suite total del proyecto de 7 a 59 pruebas que pasan con éxito):

1. **Pruebas de Dominio (`CouponTest.cs`)**:
   - Inicialización correcta de constructor y método factory `Coupon.Create`.
   - Fallo ante código nulo, vacío o espacios en blanco.
   - Fallo ante porcentaje de descuento `<= 0` o `> 100`.
   - Fallo ante monto mínimo negativo.
   - Fallo ante usos máximos `<= 0`.
   - Verificación de `CanBeAppliedTo` (válido, inactivo, vencido, monto insuficiente, límite de usos alcanzado).
   - Cálculo correcto del descuento porcentual y redondeo a 2 decimales.
   - Fallo en `CalculateDiscount` cuando no es aplicable.
   - Incremento de contador `TimesUsed` en `RecordUsage`.
   - Desactivación automática al alcanzar `MaxUsages`.
   - Métodos de activación, desactivación y actualización de vencimiento.
2. **Pruebas de Validación (`CreateCouponValidatorTest.cs` y `GetCouponByIdValidatorTest.cs`)**:
   - Aceptación de requests válidos.
   - Rechazo de códigos vacíos, menores a 3 caracteres, mayores a 20 caracteres o con caracteres no permitidos.
   - Rechazo de porcentajes fuera del rango `(0, 100]`.
   - Rechazo de fechas de vencimiento pasadas.
   - Rechazo de identificadores menores o iguales a 0.
3. **Pruebas de Lógica de Aplicación (`GetCouponHandlerTest.cs`)**:
   - Retorno exitoso de `Result<CouponDto>` cuando el cupón existe en el repositorio (usando `NSubstitute`).
   - Retorno de `ResultStatus.NotFound` cuando el cupón no existe.
4. **Pruebas de Integración End-to-End (`CouponEndpointsIntegrationTest.cs`)**:
   - `GET /coupons/1` devuelve el cupón sembrado `WELCOME10` con código HTTP 200.
   - `POST /coupons` crea exitosamente un cupón con código 201 y header `Location`.
   - `GET /coupons/{id}` posterior comprueba que el cupón recién creado fue persistido y se puede consultar.
   - `POST /coupons` con datos inválidos retorna `400 Bad Request`.
   - `POST /coupons` con código repetido retorna `409 Conflict`.
   - `GET /coupons/9999` retorna `404 Not Found`.

---

## 6. Guía para la Defensa Oral (Coloquio en Equipo de 3)

### Rol / Integrante 1: Diseño del Objeto de Dominio y Principios DDD
- **Tema**: Explicar por qué se eligió `Coupon`, por qué no duplica entidades previas y cómo se modeló como Aggregate Root.
- **Conceptos clave**:
  - Modelo de dominio rico vs. anémico: la entidad contiene sus propias reglas de negocio (`CanBeAppliedTo`, `CalculateDiscount`, `RecordUsage`) y protege sus invariantes en el constructor.
  - Uso de Value Object para `CouponId` (con Vogen) para evitar "primitive obsession".
  - Encapsulación: setters privados y método factory `Coupon.Create(...)`.

### Rol / Integrante 2: Recorrido de la Solicitud y Persistencia (Flujo End-to-End)
- **Tema**: Recorrido de una solicitud HTTP desde el endpoint hasta la base de datos SQLite.
- **Conceptos clave**:
  - **FastEndpoints (REPR Pattern)**: `CreateCouponEndpoint` y `GetCouponByIdEndpoint` actúan como entrada HTTP desacoplada.
  - **CQRS con Mediator**: En la consulta, el endpoint despacha `GetCouponQuery`, desacoplando la capa web del caso de uso.
  - **Patrón Especificación y Repositorio**: `CouponByIdSpec` y `CouponByCodeSpec` junto con `IRepository<Coupon>` y `IReadRepository<Coupon>`.
  - **Persistencia**: EF Core con `CouponConfiguration`, generador de claves `VogenIntIdValueGenerator` con `HasSentinel(CouponId.New)`, y migración `AddCoupon`.
  - **Arquitectura de dependencias**: Regla en `config.nsdepcop` asegurando que las Features no dependan de la infraestructura concreta.

### Rol / Integrante 3: Estrategia de Validación y Pruebas Unitarias/Integrales
- **Tema**: Decisiones de validación y qué verifica cada capa de pruebas.
- **Conceptos clave**:
  - **Validación en dos niveles**:
    1. *Validación de entrada (FluentValidation)* en los endpoints: formato, tipos, rangos de datos antes de invocar la lógica.
    2. *Invariantes de negocio (Guard Clauses y lógica interna)* en el Dominio: asegura que la entidad nunca quede en un estado inválido, independientemente de quién la invoque.
  - **Tipos de prueba**:
    - Pruebas unitarias de dominio (`CouponTest`): prueban la lógica pura de negocio sin base de datos ni dependencias externas.
    - Pruebas de validadores (`CreateCouponValidatorTest`): prueban todas las ramas de error y reglas sintácticas.
    - Pruebas de aplicación (`GetCouponHandlerTest`): prueban el query handler aislando el repositorio con mocks (`NSubstitute`).
    - Pruebas de integración (`CouponEndpointsIntegrationTest`): prueban el pipeline completo de ASP.NET Core con `WebApplicationFactory`.
