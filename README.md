# SGINVENTARIO

SGINVENTARIO es una aplicación Blazor Web App (Server) sobre .NET 9 que permite administrar el inventario de equipos de cómputo, controlando asignaciones, mantenimientos y reportes. Utiliza Entity Framework Core con SQL Server y ASP.NET Core Identity para la autenticación y autorización por roles.

## Requisitos

- .NET SDK 9.0.100 o superior
- SQL Server 2019+ (administrado con SSMS 21)
- Visual Studio 2022 17.10+ o Visual Studio Code

## Configuración inicial

1. Clonar el repositorio y abrir la solución `SGINVENTARIO.sln` en Visual Studio 2022.
2. Actualizar la cadena de conexión `DefaultConnection` en `SGINVENTARIO/appsettings.json` y `appsettings.Development.json` para que apunte a la instancia de SQL Server correspondiente (por ejemplo `Server=DESKTOP\\SQLEXPRESS;Database=SGInventarioDB;Trusted_Connection=True;TrustServerCertificate=True`).
3. Abrir la Consola del Administrador de Paquetes y ejecutar:

   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```

   Esto crea la base de datos, tablas e inserta los catálogos iniciales y el usuario administrador (`admin@demo.com` / `Admin#123`).

4. Ejecutar la aplicación (`F5`). El sitio se alojará en `https://localhost:xxxx`.

## Características principales

- Dashboard con métricas de equipos, gráfico de estados, próximos mantenimientos y últimos movimientos.
- CRUD completo de equipos con filtros, orden, paginación, importación CSV y exportación CSV/PDF.
- Registro automático de movimientos ante asignaciones, traslados y cambios de estado con control de concurrencia por `RowVersion`.
- Gestión de mantenimientos preventivos/correctivos con seguimiento de estado, coste y fecha de ejecución.
- Historial de movimientos filtrable por fecha, tipo y equipo.
- Catálogos de Áreas, Plantas y UsuariosPersona.
- Alertas por garantías y mantenimientos próximos en la barra de navegación.
- Roles predefinidos: `Admin`, `Tecnico`, `Consulta`.

## Scripts útiles

- Ejecutar la aplicación: `dotnet run --project SGINVENTARIO/SGINVENTARIO.csproj`
- Compilar: `dotnet build SGINVENTARIO/SGINVENTARIO.csproj`
- Ejecutar migraciones desde CLI:

  ```bash
  dotnet ef migrations add InitialCreate --project SGINVENTARIO/SGINVENTARIO.csproj
  dotnet ef database update --project SGINVENTARIO/SGINVENTARIO.csproj
  ```

## Credenciales iniciales

- Usuario administrador: `admin@demo.com`
- Contraseña: `Admin#123`

Se recomienda cambiar la contraseña después del primer inicio de sesión.
