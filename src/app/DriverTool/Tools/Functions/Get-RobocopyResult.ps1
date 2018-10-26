function Get-RoboCopyResult {
    param(
        [Parameter(Mandatory = $true)]
        [int]
        $RoboCopyExitCode
    )
    $exitCode = 0
    switch ( $RoboCopyExitCode ) {
        16 { 
            Write-Log -Level INFO "Robocopy result 16: ***FATAL ERROR***"
            $exitCode = 1   
        }
        15 { 
            Write-Log -Level INFO "Robocopy result 15: OKCOPY + FAIL + MISMATCHES + XTRA"
            $exitCode = 1    
        }
        14 { 
            Write-Log -Level INFO "Robocopy result 14: FAIL + MISMATCHES + XTRA"
            $exitCode = 1    
        }
        13 { 
            Write-Log -Level INFO "Robocopy result 13: OKCOPY + FAIL + MISMATCHES"
            $exitCode = 1    
        }
        12 { 
            Write-Log -Level INFO "Robocopy result 12: FAIL + MISMATCHES"
            $exitCode = 1    
        }
        11 { 
            Write-Log -Level INFO "Robocopy result 11: OKCOPY + FAIL + XTRA"
            $exitCode = 1    
        }
        10 { 
            Write-Log -Level INFO "Robocopy result 10: FAIL + XTRA"
            $exitCode = 1    
        }
        9 { 
            Write-Log -Level INFO "Robocopy result 9: OKCOPY + FAIL"
            $exitCode = 1    
        }
        8 { 
            Write-Log -Level INFO "Robocopy result 8: FAIL"
            $exitCode = 1    
        }
        7 { 
            Write-Log -Level INFO "Robocopy result 7: OKCOPY + MISMATCHES + XTRA"
            $exitCode = 0    
        }
        6 { 
            Write-Log -Level INFO "Robocopy result 6: MISMATCHES + XTRA"
            $exitCode = 0    
        }
        5 { 
            Write-Log -Level INFO "Robocopy result 5: OKCOPY + MISMATCHES"
            $exitCode = 0    
        }
        4 { 
            Write-Log -Level INFO "Robocopy result 4: MISMATCHES"
            $exitCode = 0    
        }
        3 { 
            Write-Log -Level INFO "Robocopy result 3: OKCOPY + XTRA"
            $exitCode = 0    
        }
        2 { 
            Write-Log -Level INFO "Robocopy result 2: XTRA"
            $exitCode = 0    
        }
        1 { 
            Write-Log -Level INFO "Robocopy result 1: OKCOPY"
            $exitCode = 0    
        }
        0 { 
            Write-Log -Level INFO "Robocopy result 0: No Change"
            $exitCode = 0    
        }
    }
    return $exitCode
}

<#
TEST:
Get-RoboCopyResult -RoboCopyExitCode 0
Get-RoboCopyResult -RoboCopyExitCode 1
Get-RoboCopyResult -RoboCopyExitCode 2
Get-RoboCopyResult -RoboCopyExitCode 3
Get-RoboCopyResult -RoboCopyExitCode 4
Get-RoboCopyResult -RoboCopyExitCode 5
Get-RoboCopyResult -RoboCopyExitCode 6
Get-RoboCopyResult -RoboCopyExitCode 7
Get-RoboCopyResult -RoboCopyExitCode 8
Get-RoboCopyResult -RoboCopyExitCode 9
Get-RoboCopyResult -RoboCopyExitCode 10
Get-RoboCopyResult -RoboCopyExitCode 11
Get-RoboCopyResult -RoboCopyExitCode 12
Get-RoboCopyResult -RoboCopyExitCode 13
Get-RoboCopyResult -RoboCopyExitCode 14
Get-RoboCopyResult -RoboCopyExitCode 15
Get-RoboCopyResult -RoboCopyExitCode 16
#>


