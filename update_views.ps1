$map = @{
    "Acceso" = "Accesos"; "Carrito" = "Carritos"; "Categoria" = "Categorias";
    "Cupon" = "Cupones"; "DetallePedido" = "DetallesPedidos"; "Pedido" = "Pedidos";
    "Producto" = "Productos"; "Restaurante" = "Restaurantes"; "Rol" = "Roles";
    "Usuario" = "Usuarios"; "Valoracion" = "Valoraciones"; "Ubicacion" = "Ubicaciones"
}

$files = Get-ChildItem -Path Proyecto/Views -Recurse -Filter *.cshtml
foreach ($f in $files) {
    if ($f.Name -match "^_") { continue } # Skip _Layout, _Header temporalmente para tratarlos manual si ocupamos, o dejarlos

    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $modified = $false
    foreach ($k in $map.Keys) {
        $v = $map[$k]
        if ($content.Contains("asp-controller=`"$k`"")) {
            $content = $content.Replace("asp-controller=`"$k`"", "asp-controller=`"$v`"")
            $modified = $true
        }
        if ($content.Contains("href=`"/$k/")) {
            $content = $content.Replace("href=`"/$k/", "href=`"/$v/")
            $modified = $true
        }
    }
    if ($modified) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.Encoding]::UTF8)
    }
}

$layoutFiles = @("Proyecto/Views/Shared/_Layout.cshtml", "Proyecto/Views/Shared/_Header.cshtml")
foreach ($f in $layoutFiles) {
    if (Test-Path $f) {
        $content = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
        foreach ($k in $map.Keys) {
            $v = $map[$k]
            $content = $content.Replace("asp-controller=`"$k`"", "asp-controller=`"$v`"")
            $content = $content.Replace("href=`"/$k/", "href=`"/$v/")
            $content = $content.Replace("@ClaseActiva(`"$k`")", "@ClaseActiva(`"$v`")")
        }
        [System.IO.File]::WriteAllText($f, $content, [System.Text.Encoding]::UTF8)
    }
}
