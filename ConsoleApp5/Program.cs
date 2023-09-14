using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleApp5
{
    class Program
    { 
        // Класс для хранения параметров запроса 
        public class Request {

            [JsonProperty("version")]
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

            [JsonProperty("authLogin")]
            public string AuthLogin { get; set; }

            [JsonProperty("secure")]
            public string Secure { get; set; }
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

        // Метод для вычисления значения поля secure по алгоритму СДЭК
        public static string GetSecure(string date, string password)
        {
            // Сконкатенируем дату и пароль с помощью символа амперсанда (&)
            var input = date + "&" + password;

            // Создаем объект MD5CryptoServiceProvider для вычисления хеша MD5 от строки input
            using (var md5 = new MD5CryptoServiceProvider())
            {
                // Преобразуем строку input в массив байтов
                var bytes = Encoding.UTF8.GetBytes(input);

                // Вычисляем хеш MD5 от массива байтов
                var hash = md5.ComputeHash(bytes);

                // Преобразуем массив байтов в строку в шестнадцатеричном формате
                var output = BitConverter.ToString(hash).Replace("-", "").ToLower();

                // Возвращаем полученную строку
                return output;
            }
        }

        static async Task Main(string[] args)
        {
            // Создаем объект Request с нужными параметрами
            var request = new Request()
            {
                Version = "1.0",
                DateExecute = DateTime.Now.ToString("yyyy-MM-dd"),
                SenderCityId = "137", // ФИАС код города Санкт-Петербурга
                ReceiverCityId = "44", // ФИАС код города Москвы
                TariffId = 2, // Код тарифа курьерской доставки СДЭК
                Goods = new Good[]
                {
                new Good()
                {
                    Weight = 0.1, // Вес в граммах
                    Length = 10.22, // Длина в миллиметрах
                    Width = 20.66, // Ширина в миллиметрах
                    Height = 100 // Высота в миллиметрах
                }
                },

                // Присваиваем свойству AuthLogin значение идентификатора аккаунта
                AuthLogin = "EMscd6r9JnFiQ3bLoyjJY6eM78JrJceI",

                // Вычисляем и присваиваем свойству Secure значение секретного ключа по алгоритму СДЭК
                Secure = GetSecure(DateTime.Now.ToString("yyyy-MM-dd"), "PjLZkKBHEiLK3YsjtNrt3TGNG0ahs3kG")
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
