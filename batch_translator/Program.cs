using batch_translator;
using BatchTranslator;
using CsvHelper;
using System.Globalization;
using System.Net;
using System.Text;

ConsoleApp.Run<MyCommands>(args);

public class MyCommands : ConsoleAppBase
{
	[RootCommand]
	public void RootCommand(
		[Option("i", "Filepath of the source text")] string inputFilePath,
		[Option("o", "Filepath of the output text")] string outputFilePath,
		[Option("k", "Filepath of API key-secret pair (csv)")] string apiFilePath,
		[Option("s", "Language of the source text")] string sourceLanguage,
		[Option("t", "Language of the output text")] string targetLanguage)
	{// -i "C:\BIN\input.txt" -o "C:\BIN\output.txt" -k "C:\BIN\api.csv" -s ja -t ko
		var arguments = Context.Arguments;
		Console.WriteLine($"Executing with arguments: \"{string.Join(" ", arguments)}\"");

		sk = sourceLanguage;
		tk = targetLanguage;

		Console.WriteLine("Welcome to BatchTranslator");
		Console.WriteLine("Initializing WebClient");
		InitWebClient(apiFilePath);
		TranslateTextFile(inputFilePath, outputFilePath);
	}

	private static List<ApiPair> apiPairs;
	private static int apiIndex = 0;
	private static void InitWebClient(string apiFilePath)
	{
		using var reader = new StreamReader(apiFilePath);
		using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
		apiPairs = csv.GetRecords<ApiPair>().ToList();
	}

	private static void TranslateTextFile(string inputFilePath, string outputFilePath)
	{
		Console.WriteLine($"Reading from {inputFilePath}, result will be saved to {outputFilePath}");

		// If file at outputFilePath exists, save the index of last row
		int lastRow = -1;
		if (File.Exists(outputFilePath))
		{
			lastRow = 0;
			Console.WriteLine("Found existing output file. Checking if it is up to date.");

			using var outputFileRaader = File.OpenText(outputFilePath);
			while (outputFileRaader.ReadLine() != null)
			{
				lastRow++;
			}

			using var inputFileReader = File.OpenText(inputFilePath);
			int inputFileRow = 0;
			while (inputFileReader.ReadLine() != null)
			{
				inputFileRow++;
			}

			if (lastRow == inputFileRow)
			{
				Console.WriteLine("The translation of input file has already been finished. Terminating the program.");
				return;
			}
			else if (lastRow > inputFileRow)
			{
				Console.WriteLine("The output file is longer than the input file. This is strange. Terminating the program.");
				return;
			}
			if (lastRow == 0)
			{
				Console.WriteLine("(The output file exists but it's empty)");
			}
			else
			{
				Console.WriteLine($"Resuming translation from row #{lastRow + 1} of {Path.GetFileName(inputFilePath)}");
			}
		}

		bool hasSpareKey;
		try
		{
			using StreamWriter sw = lastRow == 0 ? new(outputFilePath) : new(outputFilePath, true);

			var lines = File.ReadLines(inputFilePath).Skip(lastRow > -1 ? lastRow : 0);
			foreach (var line in lines)
			{
				Console.Write($"Translating {line.TrimEnd()}:");
				var translated = Translate(line, out WebException exception);
				Console.WriteLine(translated);
				if (exception != null)
				{
					Console.WriteLine($"\tTranslation failed. Detail: {exception.Message}");

					using var response = exception.Response;
					var httpResponse = (HttpWebResponse)response;

					if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
					{
						using var responseData = response.GetResponseStream();
						using var responseReader = new StreamReader(responseData, Encoding.UTF8);
						var papagoFailResponse = PapagoFailResponse.FromJson(responseReader.ReadToEnd());
						Console.WriteLine($"\tResponse message: {papagoFailResponse.ErrorMessage}");
						break;
					}
					else if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
					{// 
						using var responseData = response.GetResponseStream();
						using var responseReader = new StreamReader(responseData, Encoding.UTF8);
						var papagoFailResponse = PapagoFailResponse.FromJson(responseReader.ReadToEnd());
						Console.WriteLine($"\tResponse message: {papagoFailResponse.ErrorMessage}");

						bool noApiKey = false;
						do
						{
							Console.WriteLine("\tMaybe have reached API limit. Trying to use spare API key.)");
							hasSpareKey = NextApiKey();
							if (hasSpareKey)
							{
								translated = Translate(line, out exception);
								if (exception == null)
								{
									Console.Write($"\tTranslating {line.TrimEnd()}:");
									Console.WriteLine(translated);
								}
							}
							else
							{
								Console.WriteLine("API keys have been run out. Add more keys or try again tomorrow.");
								noApiKey = true;
								break;
							}
						} while (translated == string.Empty);
						if (noApiKey) break;
					}
					else if (httpResponse.StatusCode == HttpStatusCode.InternalServerError)
					{
						Console.WriteLine($"\tInternal Server error: {httpResponse.StatusDescription}");
						break;
					}
				}

				sw.WriteLine(translated);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"The program terminated with exception {ex.Message}: {ex.StackTrace}");
		}
	}

	private static bool NextApiKey()
	{
		if (apiPairs.Count() == apiIndex + 1) return false;
		apiIndex++;
		return true;
	}

	private static string sk = "ja";
	private static string tk = "ko";
	private static string url = "https://openapi.naver.com/v1/papago/n2mt";

	private static string Translate(string sentence, out WebException exception)
	{
		string translated = string.Empty;

		HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
		webRequest.Headers.Add("X-Naver-Client-Id", apiPairs[apiIndex].ClientId);
		webRequest.Headers.Add("X-Naver-Client-Secret", apiPairs[apiIndex].ClientSecret);
		webRequest.Method = "POST";

		byte[] byteDataParams = Encoding.UTF8.GetBytes($"source={sk}&target={tk}&text=" + sentence);
		webRequest.ContentType = "application/x-www-form-urlencoded";
		webRequest.ContentLength = byteDataParams.Length;
		using Stream st = webRequest.GetRequestStream();
		st.Write(byteDataParams, 0, byteDataParams.Length);

		try
		{
			using HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
			if (response.StatusCode == HttpStatusCode.OK)
			{
				using Stream stream = response.GetResponseStream();
				using StreamReader reader = new(stream, Encoding.UTF8);
				string responseString = reader.ReadToEnd();
				var papagoResponse = PapagoResponse.FromJson(responseString);

				translated = papagoResponse.Message.Result.TranslatedText;
			}
			else
			{
				translated = string.Empty;
			}
			exception = null;
		}
		catch (WebException ex)
		{
			exception = ex;
		}

		return translated;
	}
}
