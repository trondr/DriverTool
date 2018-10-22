let getPrefixes count =
        Array.init count (fun index -> ((index+1)*10).ToString("D3"))
    
(getPrefixes 10)
