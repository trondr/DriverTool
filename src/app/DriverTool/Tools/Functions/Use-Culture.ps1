function Use-Culture {
    param (
        
        [Parameter(Mandatory=$true)]
        [System.Globalization.CultureInfo]
        $Culture=(throw "USAGE: Use-Culture -Culture culture -Script {scriptblock}"),

        [Parameter(Mandatory=$true)]
        [ScriptBlock]
        $Script=(throw "USAGE: Use-Culture -Culture culture -Script {scriptblock}")
    )

    $OldCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture
    trap 
    {
        [System.Threading.Thread]::CurrentThread.CurrentCulture = $OldCulture
    }
    [System.Threading.Thread]::CurrentThread.CurrentCulture = $Culture
    Invoke-Command $Script
    [System.Threading.Thread]::CurrentThread.CurrentCulture = $OldCulture
}

#TEST: Use-Culture -Culture "en-US" -Script {$(Get-Date).ToString("HH:mm:ss")}
