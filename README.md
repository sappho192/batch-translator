# batch-translator
Batch translator which uses Papago NMT(Neural Machine Translation) API

# How to use
Usage: batch_translator [options...]

Options:
- -i, --input-file-path `<String>`:     Filepath of the source text (Required)  
- -o, --output-file-path `<String>`:    Filepath of the output text (Required)  
- -k, --api-file-path `<String>`:       Filepath of API key-secret pair (csv) (Required)  
- -s, --source-language `<String>`:     Language of the source text (Required)  
- -t, --target-language `<String>`:     Language of the output text (Required)  

The content of `api.csv` would be written like below:
```csv
ClientId,ClientSecret
YOUR_API_ID_1,YOUR_API_SECRET_1
YOUR_API_ID_2,YOUR_API_SECRET_2
YOUR_API_ID_3,YOUR_API_SECRET_3
...
```
The program will automatically try to switch the API key to another if current API key reaches daily limit.

The content input text would be written like below( which means each sentences should be separated with line breaks):
```txt
Good morning!
Good afternoon.
The content should be ready until Saturday.
...
```

## Windows
```Powershell
.\batch_translator.exe -i "C:\BIN\input.txt" -o "C:\BIN\output.txt" -k "C:\BIN\api.csv" -s ja -t ko
```

## Linux or macOS
```bash
./batch_translator -i "input.txt" -o "output.txt" -k "api.csv" -s ja -t ko
```