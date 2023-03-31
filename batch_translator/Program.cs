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
		sk = sourceLanguage;
		tk = targetLanguage;

		Console.WriteLine("Welcome to BathTranslator");
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

	private void TranslateTextFile(string inputFilePath, string outputFilePath)
	{
		Console.WriteLine($"Reading from {inputFilePath}, saving result to {outputFilePath}");
		bool hasSpareKey;
		try
		{
			using StreamWriter sw = new(outputFilePath);

			var lines = File.ReadLines(inputFilePath);
			foreach (var line in lines)
			{
				Console.Write($"Translating {line.TrimEnd()}:");
				var translated = Translate(line, out WebException exception);
				Console.WriteLine(translated);
				if (exception != null)
				{
					Console.WriteLine($"Translation failed. Detail: {exception.Message}");
					bool noApiKey = false;
					do
					{
						Console.WriteLine("Trying to use spare API key. (Maybe reached API limit?)");
						hasSpareKey = NextApiKey();
						if (hasSpareKey)
						{
							translated = Translate(line, out exception);
							if (exception == null)
							{
								Console.WriteLine(translated);
							}
						}
						else
						{
							Console.WriteLine("API key has been run out");
							noApiKey = true;
							break;
						}
					} while (translated == string.Empty);
					if (noApiKey) break;
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
