# TP Microservicios eCommerce — Grupo 13

Sistema de E-Commerce basado en arquitectura de microservicios, implementado en C# con .NET Core 8.

Diagrama de la arquitectura del eCommerce: 
<img width="1042" height="432" alt="arquitectura-ecommerce drawio" src="https://github.com/user-attachments/assets/5140bfd6-a79e-4e32-ae7f-45ad9e491f59" />


## Integrantes

| Integrante | Servicios |
|---|---|
| Dani | Products.API, Cart.API |
| Bauti | Orders.API |
| Pau | Users.API, Notifications.API |

---

## Requisitos

- .NET 8 SDK — [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

---

## Cómo ejecutar el proyecto

### 1. Clonar el repositorio

```bash
git clone https://github.com/bautistacarpani/TP-Microservicios-eCommerce-Grupo-13.git
cd TP-Microservicios-eCommerce-Grupo-13
```

### 2. Levantar cada servicio

Cada servicio se levanta por separado en su propia terminal.

> **Importante:** Para poder validar las conexiones entre todas las APIs, se recomienda levantar todas antes de probar.

**Products.API**
```bash
cd EcommerceMicroservicios/Products.API
dotnet run
```
Swagger: https://localhost:7209/swagger — Health UI: http://localhost:7209/health-ui
Swagger: http://localhost:5046/swagger — Health UI: http://localhost:5046/health-ui

**Cart.API**
```bash
cd EcommerceMicroservicios/Cart.API
dotnet run
```
Swagger: http://localhost:7095/swagger — Health UI: http://localhost:7095/health-ui
Swagger: http://localhost:5252/swagger — Health UI: http://localhost:5252/health-ui

**Users.API**
```bash
cd EcommerceMicroservicios/Users.API
dotnet run
```
Swagger: http://localhost:7203/swagger — Health UI: http://localhost:7203/health-ui
Swagger: http://localhost:5242/swagger — Health UI: http://localhost:5242/health-ui

**Orders.API**
```bash
cd EcommerceMicroservicios/Orders.API
dotnet run
```
Swagger: http://localhost:7002/swagger — Health UI: http://localhost:7002/health-ui
Swagger: http://localhost:5016/swagger — Health UI: http://localhost:5016/health-ui

**Notifications.API**
```bash
cd EcommerceMicroservicios/Notifications.API
dotnet run
```
Swagger: http://localhost:7041/swagger — Health UI: http://localhost:7041/health-ui
Swagger: http://localhost:5270/swagger — Health UI: http://localhost:5270/health-ui

> **Nota:** Los puertos indicados corresponden a los configurados en el archivo `Properties/launchSettings.json` de cada API. Si necesitás cambiarlos, modificá el campo `applicationUrl` en ese archivo antes de correr el proyecto.

###1. Propósito del Sistema

Este proyecto implementa un sistema de E-Commerce basado en una arquitectura de microservicios. Cada funcionalidad del sistema está diseñada de forma desacoplada y se expone como una REST API independiente, desarrollada en C# utilizando .NET Core 8.

El ecosistema se compone de 5 microservicios con responsabilidades únicas y bien definidas:
*   **Products.API:** Gestiona el catálogo de productos.
*   **Users.API:** Gestiona la identidad, el registro y la autenticación (login) de los usuarios.
*   **Orders.API:** Gestiona las órdenes de compra de los clientes.
*   **Cart.API:** Administra de forma temporal los carritos de compras.
*   **Notifications.API:** Gestiona y simula el envío de alertas y notificaciones a los usuarios.

A nivel de persistencia, todas las APIs utilizan **SQLite** como motor de base de datos relacional embebido, junto con **Dapper** como micro-ORM para garantizar consultas asíncronas y ultrarrápidas.

---

### 2. Instrucciones de Ejecución

El proyecto está diseñado para ser fácilmente ejecutable en entornos de desarrollo. No es necesario instalar ni configurar ningún motor de base de datos externo, ya que los microservicios generan su propia infraestructura local.

**Paso a paso para iniciar cualquier microservicio:**

1.  Abrir una terminal o consola de comandos en la raíz del proyecto.
2.  Navegar hacia la carpeta específica del microservicio que se desea levantar (por ejemplo: `cd src/Users.API/` o `cd src/Products.API/`).
3.  Ejecutar el siguiente comando:
    ```bash
    dotnet run
    ```
4.  El SDK de .NET leerá el archivo `.csproj`, restaurará los paquetes necesarios, compilará el código y levantará el servidor web local. En la terminal se indicará la URL y el puerto de escucha (ej. `Now listening on: http://localhost:5000`).

**Generación Automática de la Base de Datos:**
Al ejecutar `dotnet run` por primera vez en cada API, la clase interna `DatabaseInitializer` se dispara automáticamente. Este componente crea el archivo físico de SQLite (por ejemplo, `users.db`, `products.db` o `cart.db`) en la raíz del microservicio y ejecuta el código SQL necesario para generar todas las tablas. Si se apaga la API, los datos persisten en estos archivos de forma segura.

**Acceso a las herramientas (Una vez iniciado el servicio):**
*   **Documentación Interactiva (Swagger UI):** Navegar a `http://localhost:[PUERTO]/swagger` desde el navegador. Aquí se pueden visualizar todos los endpoints, ver ejemplos de requests/responses, el catálogo de errores, y probar la API en vivo.
*   **Monitoreo (Health Checks):** Navegar a `http://localhost:[PUERTO]/health` para ver el JSON de estado del servicio, o ingresar a `http://localhost:[PUERTO]/health-ui` para visualizar el tablero de control de salud del contenedor y de la conexión con SQLite.

### 3. Características Transversales y Observabilidad

Para garantizar que el ecosistema de microservicios sea tolerante a fallos y fácil de auditar en un entorno de producción, todas las APIs implementan las siguientes características:

*   **Monitoreo (Health Checks):** La salud del sistema se puede verificar en tiempo real consultando tres sondas especializadas
    *   `/health`: Devuelve un JSON con el estado general y detallado del ecosistema *   `/health/ready`: Evalúa si la conexión a la base de datos (SQLite) está lista para procesar tráfico
    *   `/health/live`: Indica el estado del proceso y si el contenedor sigue vivo 
    *   **Dashboard Visual:** También se puede acceder a la interfaz gráfica amigable ingresando a la ruta `/health-ui` desde el navegador 

*   **Trazabilidad (Correlation ID):** La API captura automáticamente la cabecera `X-Correlation-Id` (o genera una nueva) y la inyecta tanto en las peticiones HTTP salientes como en los logs. Estos registros se guardan localmente en formato JSON estructurado dentro del archivo `logs/audit.log`. Esto es vital para soporte técnico, ya que permite rastrear con precisión milimétrica el flujo completo de una solicitud o un error a través de todos los microservicios implicados 

# 👤 Users API - Microservicio de Identidad y Autenticación

## 1. Modelo de Datos
La entidad principal de este servicio es el `User`, el cual se almacena en su propia base de datos SQLite aislando su información del resto del sistema. Sus campos principales incluyen:
* **Id**: Identificador único (manejado en el código como `string` para evitar conflictos de mapeo nativos entre SQLite y Dapper).
* **Datos Personales**: Nombre, Apellido y Email (único).
* **Seguridad**: PasswordHash, Activo (booleano) e IntentosFallidos.
* **Auditoría**: FechaRegistro.

## 2. Endpoints Disponibles
* **`POST /api/users/register`**: Para registrar un usuario nuevo. Valida que el email no esté duplicado y encripta la contraseña. Envía automáticamente un mail de bienvenida (puede estar en SPAM).
* **`POST /api/users/login`**: Para autenticar mediante email y contraseña. Devuelve los datos del usuario si las credenciales son correctas.
* **`GET /api/users/{id}/exists`**: Endpoint interno ultrarrápido creado para validar si un usuario existe sin exponer sus datos sensibles. Es consumido de forma cruzada por las APIs de Notifications y Orders.

## 3. Reglas de Negocio y Seguridad
Quien use la API necesita saber las validaciones lógicas que ocurren "por detrás":
* **Seguridad de Contraseñas**: El sistema convierte las contraseñas en un hash irreversible. El campo `PasswordHash` jamás se expone en ninguna respuesta HTTP.
* **Política de Bloqueo**: Si se ingresan credenciales incorrectas, el sistema incrementa el campo `IntentosFallidos`. Al acumular **3 intentos fallidos consecutivos**, la propiedad `Activo` pasa a `false` y la cuenta queda bloqueada. *(Nota de implementación: Al realizar un login exitoso, el sistema ejecuta una consulta en el repositorio que resetea automáticamente estos intentos a cero)* 

## 4. Contrato de Errores 
Se debe explicar que la API responde a los errores utilizando el estándar **Problem Details (RFC 7807)**, ocultando los *stack traces* en producción por seguridad. Se debe incluir el catálogo de códigos personalizados:
* **`USR-001`** (409 Conflict): El email ya está registrado.
* **`USR-002`** (400 Bad Request): Datos de usuario inválidos.
* **`USR-003`** (401 Unauthorized): Credenciales incorrectas.
* **`USR-004`** (403 Forbidden): Usuario bloqueado por demasiados intentos.
* **`USR-005`** (403 Forbidden): Usuario bloqueado por fraude.
* **`USR-006`** (500 Internal Server Error): Error interno inesperado.

# 🔔 Notifications API - Microservicio de Alertas y Notificaciones

## 1. Modelo de Datos
Sus campos incluyen:
* **Id**: Identificador único de la alerta.
* **UsuarioId**: Referencia al usuario destinatario. Trabaja bajo la premisa de "Cero Datos Fantasma", garantizando que nunca se registre una alerta destinada a un usuario que no existe en el sistema
* **Mensaje**: El contenido a enviar (máximo 500 caracteres).
* **Tipo**: El canal de envío (`Email`, `Push` o `SMS`).
* **Estado**: Situación de la alerta (`Pendiente`, `Enviada` o `Fallida`).
* **FechaEnvio**: Fecha asignada automáticamente al registrarse.

## 2. Endpoints Disponibles
* **`POST /api/notifications/send`**: Registra y envía una nueva notificación a través de Email (suele llegar en SPAM). Recibe el destinatario, el mensaje y el tipo de canal.
* **`GET /api/notifications/{userId}`**: Lista el historial completo de notificaciones asociadas a un usuario específico.

## 3. Reglas de Negocio y Validación Cruzada
* **Aislamiento y Verificación Real**: La API de Notificaciones no posee una copia de la base de datos de los clientes. Cuando recibe la orden de enviar una notificación, "congela" su proceso un instante y se comunica por red con la API de Usuarios para verificar si el destinatario realmente existe. 
* **Tolerancia a fallos**: Si el sistema comercial intenta disparar alertas a códigos inventados o eliminados, la API bloquea la acción en el primer segundo y no guarda el registro en SQLite, ahorrando espacio y evitando "correos huérfanos".

## 4. Contrato de Errores (Catálogo)
Ante cualquier falla, el sistema devuelve una respuesta estructurada (Problem Details - RFC 7807), ocultando las trazas de pila técnicas.
* **`NTF-001`** (404 Not Found): Usuario no encontrado. Ocurre si la API de Usuarios responde que el destinatario no existe.
* **`NTF-002`** (400 Bad Request): Los datos de la notificación son inválidos. (Ej. campos vacíos o tipo de canal no reconocido).
* **`NTF-003`** (404 Not Found): No se encontraron notificaciones registradas para ese usuario al consultar el historial.
* **`NTF-004`** (500 Internal Server Error): Error interno inesperado al procesar la notificación.

## 5. Interconexión Arquitectónica y Ejecución Simultánea (⚠️ Importante)
Al estar en una arquitectura distribuida, los microservicios dependen unos de otros para validar reglas de negocio sin romper su aislamiento. 
Para poder probar exitosamente el endpoint `POST /api/notifications/send`, es **obligatorio ejecutar en simultáneo la Notifications.API y la Users.API** en terminales separadas. 
Esto se debe ya que al intentar guardar la alerta, el código de la Notifications API utiliza `IHttpClientFactory` para hacer una llamada HTTP síncrona ("por detrás") al endpoint `GET /api/users/{id}/exists` 
* Si la **Users API** no está corriendo, la llamada de red fallará, la validación no podrá completarse y el sistema abortará la operación devolviendo un error. 
* En esta comunicación interna, la API de Notificaciones también inyecta el `X-Correlation-Id` en las cabeceras HTTP, lo que permite que en los archivos de `logs/audit.log` de ambos microservicios quede registrado el viaje exacto de esa validación cruzada.

# 🛍️ Products API - Microservicio de Catálogo de Productos

## 1. Modelo de Datos
La entidad principal es `Product`, almacenada en su propia base de datos SQLite. Sus campos incluyen:
* **Id**: Identificador único (Guid).
* **Name**: Nombre del producto.
* **Description**: Descripción opcional.
* **Price**: Precio del producto.
* **Stock**: Cantidad disponible en inventario.
* **Category**: Categoría del producto (informativa, no validada contra lista fija).
* **CreatedAt / UpdatedAt**: Fechas de auditoría.

## 2. Endpoints Disponibles
* **`GET /api/products`**: Lista todos los productos. Acepta filtros opcionales por `?categoria=` y `?nombre=`.
* **`GET /api/products/{id}`**: Obtiene el detalle de un producto específico por su ID.
* **`POST /api/products`**: Crea un nuevo producto en el catálogo.
* **`PUT /api/products/{id}`**: Actualiza los datos de un producto existente.
* **`DELETE /api/products/{id}`**: Elimina un producto. Antes de eliminar, consulta Orders.API para verificar que no tenga órdenes activas asociadas.

## 3. Reglas de Negocio
* **Nombre único por categoría**: No se puede registrar dos productos con el mismo nombre dentro de la misma categoría.
* **Validación de órdenes activas**: Al intentar eliminar un producto, la API consulta Orders.API vía HTTP. Si el producto tiene órdenes activas, la eliminación es bloqueada.
* **Stock no negativo**: El stock no puede ser menor a cero al crear o actualizar.

## 4. Interconexión Arquitectónica
Esta API es consumida internamente por Cart.API y Orders.API para validar la existencia de productos y verificar stock disponible antes de procesar operaciones. Para probar el `DELETE /api/products/{id}` correctamente es recomendable tener Orders.API corriendo en simultáneo; de lo contrario la verificación de órdenes activas se omite y el producto se elimina directamente.

## 5. Contrato de Errores
* **`PRD-001`** (404 Not Found): Producto no encontrado.
* **`PRD-002`** (400 Bad Request): Datos del producto inválidos.
* **`PRD-003`** (409 Conflict): Ya existe un producto con ese nombre en la categoría.
* **`PRD-004`** (409 Conflict): El producto tiene órdenes activas y no puede eliminarse.
* **`PRD-005`** (500 Internal Server Error): Error interno inesperado.

---

# 🛒 Cart API - Microservicio de Carrito de Compras

## 1. Modelo de Datos
El carrito se divide en dos tablas SQLite relacionadas:
* **carts**: Un registro por usuario, identificado por `userId` (Guid). Incluye la fecha de última actualización.
* **cart_items**: Los productos dentro del carrito, con `productId` y `quantity`. Un usuario puede tener múltiples items.

## 2. Endpoints Disponibles
* **`GET /api/cart/{userId}`**: Obtiene el carrito activo del usuario con todos sus items.
* **`POST /api/cart/{userId}/items`**: Agrega un producto al carrito. Si el producto ya estaba, suma la cantidad. Crea el carrito automáticamente si el usuario no tenía uno.
* **`PUT /api/cart/{userId}/items/{productId}`**: Actualiza la cantidad de un producto ya existente en el carrito.
* **`DELETE /api/cart/{userId}/items/{productId}`**: Quita un producto específico del carrito.
* **`DELETE /api/cart/{userId}`**: Vacía el carrito completo del usuario.

## 3. Reglas de Negocio
* **Validación de producto**: Antes de agregar o actualizar un item, la API consulta Products.API para verificar que el producto exista en el catálogo.
* **Validación de stock**: Si la cantidad solicitada supera el stock disponible en Products.API, la operación es bloqueada.
* **Carrito automático**: Al hacer POST de un item, si el usuario no tenía carrito, se crea automáticamente.
* **Cantidad válida**: La cantidad siempre debe ser mayor a cero.

## 4. Interconexión Arquitectónica 
Para probar exitosamente los endpoints `POST` y `PUT`, es **obligatorio tener Products.API corriendo en simultáneo**. Cart.API realiza llamadas HTTP internas a Products.API para validar existencia y stock. Si Products.API no está disponible, la operación fallará.

## 5. Contrato de Errores
* **`CRT-001`** (404 Not Found): Carrito no encontrado.
* **`CRT-002`** (404 Not Found): Producto no encontrado en Products.API.
* **`CRT-003`** (422 Unprocessable Entity): Stock insuficiente para agregar al carrito.
* **`CRT-004`** (400 Bad Request): Cantidad inválida (menor o igual a cero).
* **`CRT-005`** (500 Internal Server Error): Error interno inesperado.

# 📦 Order API - Microservicio de Creación de ordenes

## 1. Modelo de Datos
El modelo de órdenes se divide en dos entidades principales que se almacenan y relacionan en su base de datos SQLite:
* **Order**: El registro principal de la compra. Incluye el `Id` (Guid), el `usuarioId` del cliente, el `total` (calculado automáticamente), el `estado` de la orden (Pendiente, Confirmada, Enviada, Entregada, Cancelada) y la `fechaCreacion`.
* **OrderItem**: Los productos individuales dentro de la orden. Contiene el `productoId`, la `cantidad` solicitada (que debe ser mayor a cero) y el `precioUnitario`, el cual es capturado directamente del catálogo al momento de crear la factura.

## 2. Endpoints Disponibles
* **`GET /api/orders`**: Lista todas las órdenes. Soporta un filtro opcional por cliente usando `?usuarioId=`
* **`GET /api/orders/{id}`**: Obtiene el detalle completo de una orden específica, incluyendo la lista de sus items.
* **`POST /api/orders`**: Crea una nueva orden de compra. Recibe el `usuarioId` y el detalle de los productos que se quieren comprar.
* **`PUT /api/orders/{id}/status`**: Actualiza el estado de una orden existente.

## 3. Reglas de Negocio
* **Validación de identidad**: Al intentar crear una orden, el sistema exige verificar primero que el `usuarioId` exista realmente .
* **Validación de producto**: Para cada item de la orden, se verifica que el producto exista en el catálogo oficial antes de proceder  .
* **Validación de stock**: La cantidad solicitada de los productos no puede superar el stock real disponible. Si no hay stock suficiente, el sistema frena la creación de la orden  .
* **Transiciones de estado**: Las órdenes tienen un flujo lógico inmutable. No se permiten transiciones inválidas (por ejemplo, el sistema rechaza cambiar una orden "Entregada" de vuelta a un estado "Pendiente") .
* **Datos requeridos**: No se puede crear una orden con una lista de items vacía  .

## 4. Interconexión Arquitectónica
Para probar exitosamente la creación de una orden mediante el endpoint `POST /api/orders`, es **obligatorio tener Users.API y Products.API corriendo en simultáneo** junto con Orders.API.
* Orders.API realiza una llamada HTTP interna a la **Users.API** para validar que el comprador es un cliente real  . 
* Simultáneamente, realiza llamadas a la **Products.API** para constatar que cada producto existe, capturar su precio oficial actual y verificar que haya stock suficiente para cubrir la compra  . 
Si alguna de estas dos APIs auxiliares no está disponible, la transacción se abortará de inmediato.

## 5. Contrato de Errores
* **`ORD-001`** (404 Not Found): Orden no encontrada.
* **`ORD-002`** (400 Bad Request): Los datos de la orden son inválidos (campos faltantes o lista vacía)  .
* **`ORD-003`** (404 Not Found): Usuario no encontrado al crear la orden (falló la validación con Users.API)  .
* **`ORD-004`** (404 Not Found): Producto no encontrado al crear la orden (falló la validación con Products.API)  .
* **`ORD-005`** (422 Unprocessable Entity): Stock insuficiente para uno o más productos  .
* **`ORD-006`** (409 Conflict): El estado de la orden no puede ser modificado por ser una transición inválida  .
* **`ORD-007`** (500 Internal Server Error): Error interno inesperado  .

---
