using Dapper;
using Microsoft.Data.Sqlite;
using Orders.API.Models;

namespace Orders.API.Services;

public class OrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=orders.db";
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task<List<Order>> GetAllAsync(Guid? usuarioId)
    {
        using var conn = CreateConnection();

        var sql = usuarioId.HasValue
            ? "SELECT * FROM orders WHERE usuario_id = @UsuarioId"
            : "SELECT * FROM orders";

        var rows = await conn.QueryAsync(sql,
            usuarioId.HasValue ? new { UsuarioId = usuarioId.ToString() } : null);

        var orders = new List<Order>();
        foreach (var row in rows)
        {
            var order = MapOrder(row);
            order.Items = (await GetItemsAsync(conn, order.Id)).ToList();
            orders.Add(order);
        }
        return orders;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM orders WHERE id = @Id",
            new { Id = id.ToString() });

        if (row is null) return null;

        var order = MapOrder(row);
        order.Items = ((IEnumerable<OrderItem>)await GetItemsAsync(conn, order.Id)).ToList();
        return order;
    }

    public async Task<Order> CreateAsync(Order order)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync("""
            INSERT INTO orders (id, usuario_id, total, estado, fecha_creacion)
            VALUES (@Id, @UsuarioId, @Total, @Estado, @FechaCreacion)
            """,
            new
            {
                Id = order.Id.ToString(),
                UsuarioId = order.UsuarioId.ToString(),
                order.Total,
                order.Estado,
                FechaCreacion = order.FechaCreacion.ToString("o")
            });

        foreach (var item in order.Items)
        {
            await conn.ExecuteAsync("""
                INSERT INTO order_items (order_id, producto_id, cantidad, precio_unitario)
                VALUES (@OrderId, @ProductoId, @Cantidad, @PrecioUnitario)
                """,
                new
                {
                    OrderId = order.Id.ToString(),
                    ProductoId = item.ProductoId.ToString(),
                    item.Cantidad,
                    item.PrecioUnitario
                });
        }

        return order;
    }

    public async Task<Order?> UpdateStatusAsync(Guid id, string nuevoEstado)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync("""
            UPDATE orders 
            SET estado = @Estado, fecha_actualizacion = @FechaActualizacion
            WHERE id = @Id
            """,
            new
            {
                Id = id.ToString(),
                Estado = nuevoEstado,
                FechaActualizacion = DateTime.UtcNow.ToString("o")
            });

        return await GetByIdAsync(id);
    }

    private async Task<IEnumerable<OrderItem>> GetItemsAsync(
        SqliteConnection conn, Guid orderId)
    {
        var rows = await conn.QueryAsync(
            "SELECT * FROM order_items WHERE order_id = @OrderId",
            new { OrderId = orderId.ToString() });

        return rows.Select(r => new OrderItem(
            Guid.Parse((string)r.producto_id),
            (int)r.cantidad,
            (decimal)r.precio_unitario
        ));
    }

    private static Order MapOrder(dynamic row) => new()
    {
        Id = Guid.Parse((string)row.id),
        UsuarioId = Guid.Parse((string)row.usuario_id),
        Total = (decimal)row.total,
        Estado = (string)row.estado,
        FechaCreacion = DateTime.Parse((string)row.fecha_creacion),
        FechaActualizacion = row.fecha_actualizacion is null
            ? null
            : DateTime.Parse((string)row.fecha_actualizacion)
    };
    
    // Consulta interna que usa Products API para verificar si puede eliminar un producto
    public async Task<bool> TieneOrdenesActivasAsync(Guid productoId)
    {
        using var conn = CreateConnection();

        var rows = await conn.QueryAsync("""
        SELECT o.estado 
        FROM order_items oi
        JOIN orders o ON o.id = oi.order_id
        WHERE oi.producto_id = @ProductoId
    """, new { ProductoId = productoId.ToString() });

        return rows.Any(r =>
        {
            var estado = (string)r.estado;
            return estado == "Pendiente" || estado == "Confirmada";
        });
    }
}