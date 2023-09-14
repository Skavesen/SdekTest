using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleApp5
{
    class Program
    { 
        // Класс для хранения параметров запроса 
        public class Request { [JsonProperty("version")]
            public string Version { get; set; }
            [JsonProperty("dateExecute")]
            public string DateExecute { get; set; }

            [JsonProperty("senderCityId")]
            public string SenderCityId { get; set; }

            [JsonProperty("receiverCityId")]
            public string ReceiverCityId { get; set; }

            [JsonProperty("tariffId")]
            public int TariffId { get; set; }

            [JsonProperty("goods")]
            public Good[] Goods { get; set; }
        }

        // Класс для хранения параметров товара
        public class Good
        {
            [JsonProperty("weight")]
            public double Weight { get; set; }

            [JsonProperty("length")]
            public double Length { get; set; }

            [JsonProperty("width")]
            public double Width { get; set; }

            [JsonProperty("height")]
            public double Height { get; set; }
        }

        // Класс для хранения результата ответа
        public class Response
        {
            [JsonProperty("result")]
            public Result Result { get; set; }
        }

        // Класс для хранения результата расчета
        public class Result
        {
            [JsonProperty("price")]
            public double Price { get; set; }
        }

        // Метод для отправки запроса и получения ответа от API СДЭК
        public static async Task<Response> GetCdekPriceAsync(Request request)
        {
            // Создаем HttpClient для отправки HTTP-запросов
            using (var client = new HttpClient())
            {
                // Устанавливаем базовый адрес API СДЭК
                client.BaseAddress = new Uri("https://api.cdek.ru/");
                
                // Устанавливаем токен авторизации в заголовке Authorization
                //client.DefaultRequestHeaders.Add("Authorization", "Bearer Токен_Авторизации");

                // Сериализуем объект request в JSON-строку
                var json = JsonConvert.SerializeObject(request);

                // Создаем HttpContent из JSON-строки
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Отправляем POST-запрос по адресу /calculator/calculate_price_by_json.php с содержимым content
                var response = await client.PostAsync("/calculator/calculate_price_by_json.php", content);

                Console.WriteLine("Код состояния:" + response.StatusCode);
                Console.WriteLine("Описание:" + response.ReasonPhrase);

                // Если ответ успешный, то
                if (response.IsSuccessStatusCode)
                {
                    // Читаем содержимое ответа как строку
                    var result = await response.Content.ReadAsStringAsync();

                    // Десериализуем строку в объект Response
                    var price = JsonConvert.DeserializeObject<Response>(result);

                    // Возвращаем объект Response
                    return price;
                }
                else
                {
                    // Иначе возвращаем null
                    return null;
                }
            }
        }

        static async Task Main(string[] args)
        {
            // Создаем объект Request с нужными параметрами
            var request = new Request()
            {
                Version = "1.0",
                DateExecute = DateTime.Now.ToString("yyyy-MM-dd"),
                SenderCityId = "78000000000", // ФИАС код города Санкт-Петербурга
                ReceiverCityId = "77000000000", // ФИАС код города Москвы
                TariffId = 137, // Код тарифа курьерской доставки СДЭК
                Goods = new Good[]
                {
                new Good()
                {
                    Weight = 1000, // Вес в граммах
                    Length = 100, // Длина в миллиметрах
                    Width = 100, // Ширина в миллиметрах
                    Height = 100 // Высота в миллиметрах
                }
                }
            };

            // Вызываем метод GetCdekPriceAsync с объектом request и получаем объект response
            var response = await GetCdekPriceAsync(request);

            // Если объект response не null, то выводим стоимость доставки на консоль
            if (response != null)
            {
                Console.WriteLine($"Стоимость доставки: {response.Result.Price} руб.");
            }
            else
            {
                // Иначе выводим сообщение об ошибке
                Console.WriteLine("Не удалось получить стоимость доставки.");
            }
        }
    }
}
