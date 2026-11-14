$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$target = Join-Path (Split-Path $PSScriptRoot -Parent) 'HKDownloader.exe'
if (-not (Test-Path $compiler)) { throw 'The x64 .NET Framework compiler was not found. Enable .NET Framework 4.8.' }
$branding = Join-Path $PSScriptRoot 'Branding'
$logoSvg = Join-Path $branding 'HKDownloader.svg'
$logoPng = Join-Path $branding 'HKDownloader.png'
$logoIco = Join-Path $branding 'HKDownloader.ico'
$logoGenerator = Join-Path $branding 'LogoGenerator.exe'
$temporaryPlatformIco = Join-Path $branding 'HKDownloader-platform-temp.ico'
# Same accents used in PlatformPalette; bright colors use dark lettering for contrast.
$platformVariants = @(
    @{ Name = 'youtube';   Background = '#FF0000'; Foreground = '#FFFFFF' },
    @{ Name = 'twitch';    Background = '#9146FF'; Foreground = '#FFFFFF' },
    @{ Name = 'kick';      Background = '#53FC18'; Foreground = '#14171E' },
    @{ Name = 'instagram'; Background = '#E1306C'; Foreground = '#FFFFFF' },
    @{ Name = 'tiktok';    Background = '#25F4EE'; Foreground = '#14171E' },
    @{ Name = 'facebook';  Background = '#0866FF'; Foreground = '#FFFFFF' }
)
$generatorOptions = @(
    '/nologo', '/target:exe', '/platform:x64', '/optimize+', '/codepage:65001',
    ('/out:' + $logoGenerator),
    ('/reference:' + (Join-Path $framework 'System.Xml.dll')),
    ('/reference:' + (Join-Path $framework 'System.Xaml.dll')),
    ('/reference:' + (Join-Path $framework 'WPF\\WindowsBase.dll')),
    ('/reference:' + (Join-Path $framework 'WPF\\PresentationCore.dll')),
    (Join-Path $branding 'LogoGenerator.cs')
)
& $compiler $generatorOptions
if ($LASTEXITCODE -ne 0) { throw 'Logo generator compilation failed.' }
try {
    & $logoGenerator $logoSvg $logoPng $logoIco
    if ($LASTEXITCODE -ne 0) { throw 'Official SVG icon rendering failed.' }
    if (-not (Test-Path $logoPng) -or -not (Test-Path $logoIco)) { throw 'Missing generated neutral logo assets.' }
    foreach ($variant in $platformVariants) {
        $variantPng = Join-Path $branding ('HKDownloader.' + $variant.Name + '.png')
        & $logoGenerator $logoSvg $variantPng $temporaryPlatformIco $variant.Background $variant.Foreground
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $variantPng)) {
            throw "Failed to render $($variant.Name) logo from SVG."
        }
    }
} finally {
    Remove-Item -LiteralPath $logoGenerator -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporaryPlatformIco -Force -ErrorAction SilentlyContinue
}
$references = @('System.dll','System.Core.dll','System.Net.Http.dll','System.Web.Extensions.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')
$options = @('/nologo','/target:winexe','/platform:x64','/optimize+','/codepage:65001',('/out:' + $target),('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')),('/win32icon:' + $logoIco),('/resource:' + $logoPng + ',HKDownloader.Logo.png'),('/resource:' + (Join-Path $PSScriptRoot 'MainWindow.xaml') + ',MainWindow.xaml'))
foreach ($variant in $platformVariants) {
    $variantPng = Join-Path $branding ('HKDownloader.' + $variant.Name + '.png')
    $options += ('/resource:' + $variantPng + ',HKDownloader.Logo.' + $variant.Name + '.png')
}
foreach ($reference in $references) { $options += '/reference:' + (Join-Path $framework $reference) }
$options += Join-Path $PSScriptRoot 'App.cs'
$options += Join-Path $PSScriptRoot 'Locales.cs'
$options += Join-Path $PSScriptRoot 'SelfTest.cs'
$options += Join-Path $PSScriptRoot 'AppUpdater.cs'
$options += Join-Path $PSScriptRoot 'ToolIntegrity.cs'
$options += Join-Path $PSScriptRoot 'MediaCleanup.cs'
& $compiler $options
if ($LASTEXITCODE -ne 0) { throw 'Build failed. See the errors above.' }
Write-Host "Built: $target" -ForegroundColor Green
