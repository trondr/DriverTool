Set SourcePngBaseFileName=DriverTool
REM choco install imagemagick
Set ConvertExe="C:\Program Files\ImageMagick-7.1.0-Q16-HDRI\magick.exe"
%ConvertExe% %SourcePngBaseFileName%.png -resize 256x256   %SourcePngBaseFileName%-256.png
%ConvertExe% %SourcePngBaseFileName%-256.png -resize 16x16   %SourcePngBaseFileName%-16.png
%ConvertExe% %SourcePngBaseFileName%-256.png -resize 32x32   %SourcePngBaseFileName%-32.png
%ConvertExe% %SourcePngBaseFileName%-256.png -resize 64x64   %SourcePngBaseFileName%-64.png
%ConvertExe% %SourcePngBaseFileName%-256.png -resize 128x128 %SourcePngBaseFileName%-128.png
%ConvertExe% %SourcePngBaseFileName%-16.png %SourcePngBaseFileName%-32.png %SourcePngBaseFileName%-64.png %SourcePngBaseFileName%-128.png %SourcePngBaseFileName%-256.png %SourcePngBaseFileName%.ico
pause