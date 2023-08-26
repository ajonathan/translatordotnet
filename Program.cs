using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Configuration;


class Program 
{     
    static string OutputSpeechRecognitionResult(TranslationRecognitionResult translationRecognitionResult)
    {
        switch (translationRecognitionResult.Reason)
        {
            case ResultReason.TranslatedSpeech:
                Console.WriteLine($"RECOGNIZED: {translationRecognitionResult.Text}");
                foreach (var element in translationRecognitionResult.Translations)
                {
                    Console.WriteLine($"TRANSLATED: {element.Value}");
                    return element.Value;
                }
                return "";
        }
        return "";
    }


     static void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Console.WriteLine($"Speech synthesized for text: {text}");
                break;
            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
            default:
                break;
        }
    }


    async static Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

        var configuration = builder.Build();

        //Import valubles from appsettings.json
        string SpeechRecognitionLanguage = configuration["SpeechRecognitionLanguage"] ?? "";
        string AddTargetLanguage = configuration["TargetLanguage"] ?? "";
        string SpeechSynthesisVoiceName = configuration["SpeechSynthesisVoiceName"] ?? "";
        string speechKey = configuration["speechKey"] ?? "";
        string speechRegion = configuration["speechRegion"] ?? "";
        
        var speechTranslationConfig = SpeechTranslationConfig.FromSubscription(speechKey, speechRegion);        
        speechTranslationConfig.SpeechRecognitionLanguage = SpeechRecognitionLanguage;
        speechTranslationConfig.AddTargetLanguage(AddTargetLanguage);

        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechSynthesisVoiceName = SpeechSynthesisVoiceName;

        var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        var translationRecognizer = new TranslationRecognizer(speechTranslationConfig, audioConfig);

        string text = "";
        string exit = "";

        while (exit != "Exit.")
        {
            Console.WriteLine("");
            Console.WriteLine("Speak into your microphone (say exit to exit the application).");
            Console.WriteLine("");

            var translationRecognitionResult = await translationRecognizer.RecognizeOnceAsync();
            //Translated result to text
            text = OutputSpeechRecognitionResult(translationRecognitionResult);
            exit = translationRecognitionResult.Text;

            using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
            {
                var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(text);
                //Write out the translated text
                OutputSpeechSynthesisResult(speechSynthesisResult, text);
            }
        }
    }
}