# Crear carpetas de Vistas
$viewDirs = @("Accesos","Carritos","Cupones","Pedidos","Restaurantes","Ubicaciones","Usuarios","Valoraciones")
foreach ($d in $viewDirs) { New-Item -ItemType Directory -Force "Proyecto/Views/$d" | Out-Null }

# Mover vistas a nuevos directorios
$moves = @(
    @{ S="Proyecto/Views/Login/login.cshtml"; D="Proyecto/Views/Accesos/login.cshtml" },
    @{ S="Proyecto/Views/Login/olvidaste-contrasena.cshtml"; D="Proyecto/Views/Accesos/olvidaste-contrasena.cshtml" },
    @{ S="Proyecto/Views/Login/register.cshtml"; D="Proyecto/Views/Accesos/register.cshtml" },
    @{ S="Proyecto/Views/Login/restaurante-info.cshtml"; D="Proyecto/Views/Accesos/restaurante-info.cshtml" },
    @{ S="Proyecto/Views/Login/selector.cshtml"; D="Proyecto/Views/Accesos/selector.cshtml" },
    @{ S="Proyecto/Views/Navbar/carrito.cshtml"; D="Proyecto/Views/Carritos/carrito.cshtml" },
    @{ S="Proyecto/Views/Navbar/cupones.cshtml"; D="Proyecto/Views/Cupones/cupones.cshtml" },
    @{ S="Proyecto/Views/Navbar/factura.cshtml"; D="Proyecto/Views/Pedidos/factura.cshtml" },
    @{ S="Proyecto/Views/Pedido/Seguimiento.cshtml"; D="Proyecto/Views/Pedidos/Seguimiento.cshtml" },
    @{ S="Proyecto/Views/Navbar/movimientos.cshtml"; D="Proyecto/Views/Usuarios/movimientos.cshtml" },
    @{ S="Proyecto/Views/CRUDs/users-form.cshtml"; D="Proyecto/Views/Usuarios/users-form.cshtml" },
    @{ S="Proyecto/Views/Navbar/Mapa.cshtml"; D="Proyecto/Views/Ubicaciones/Mapa.cshtml" },
    @{ S="Proyecto/Views/Navbar/calificanos.cshtml"; D="Proyecto/Views/Valoraciones/calificanos.cshtml" },
    @{ S="Proyecto/Views/Home/Restaurante.cshtml"; D="Proyecto/Views/Restaurantes/Restaurante.cshtml" },
    @{ S="Proyecto/Views/Navbar/busqueda.cshtml"; D="Proyecto/Views/Home/busqueda.cshtml" }
)

foreach ($m in $moves) {
    if (Test-Path $m.S) {
        Move-Item -Path $m.S -Destination $m.D -Force
    }
}

# Renombrar Controladores
$ctrlMap = @{
    "Acceso" = "Accesos"; "Carrito" = "Carritos"; "Categoria" = "Categorias";
    "Cupon" = "Cupones"; "DetallePedido" = "DetallesPedidos"; "Pedido" = "Pedidos";
    "Producto" = "Productos"; "Restaurante" = "Restaurantes"; "Rol" = "Roles";
    "Usuario" = "Usuarios"; "Valoracion" = "Valoraciones"
}

foreach ($k in $ctrlMap.Keys) {
    $v = $ctrlMap[$k]
    $old = "Proyecto/Controllers/$k`Controller.cs"
    $new = "Proyecto/Controllers/$v`Controller.cs"
    if (Test-Path $old) {
        Move-Item -Path $old -Destination $new -Force
        $content = [System.IO.File]::ReadAllText($new)
        $content = $content.Replace("class ${k}Controller", "class ${v}Controller").Replace("public ${k}Controller", "public ${v}Controller")
        [System.IO.File]::WriteAllText($new, $content, [System.Text.Encoding]::UTF8)
    }
}

# Limpiar carpetas viejas si están vacías
$oldDirs = @("Proyecto/Views/Login", "Proyecto/Views/Navbar", "Proyecto/Views/Pedido")
foreach ($d in $oldDirs) {
    if (Test-Path $d) {
        $items = Get-ChildItem -Path $d
        if ($items.Count -eq 0) { Remove-Item $d -Force }
    }
}
