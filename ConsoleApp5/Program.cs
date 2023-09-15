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
            public int SenderCityId { get; set; }
            //public Location SenderCityId { get; set; }

            [JsonProperty("receiverCityId")]
            public int ReceiverCityId { get; set; }
            //public Location ReceiverCityId { get; set; }

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

        // Класс для хранения параметров города
        public class Location
        {
            [JsonProperty("fiasGuid")]
            public string FiasGuid { get; set; }
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
                var content = new StringContent(json, Encoding.UTF8, "application/json");
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
            var input = date + "&" + password;

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

        // Метод для получения cityCode по ФИАС коду города
        public static async Task<int> GetCityCodeByFias(string fias)
        {
            // Создаем HttpClient для отправки HTTP-запросов
            using (var client = new HttpClient())
            {
                // Устанавливаем базовый адрес API СДЭК
                client.BaseAddress = new Uri("http://integration.cdek.ru/");
                // Формируем URL запроса с параметром fiasGuid
                var url = $"/v1/location/cities/json?fiasGuid={fias}";
                // Отправляем GET-запрос по сформированному URL
                var response = await client.GetAsync(url);
                Console.WriteLine("Код состояния:" + response.StatusCode);
                Console.WriteLine("Описание:" + response.ReasonPhrase);
                // Если ответ успешный, то
                if (response.IsSuccessStatusCode)
                {
                    // Читаем содержимое ответа как строку
                    var result = await response.Content.ReadAsStringAsync();
                    // Десериализуем строку в массив объектов City
                    var cities = JsonConvert.DeserializeObject<City[]>(result);
                    // Если есть хотя бы один город, то
                    if (cities.Length > 0)
                    {
                        // Возвращаем cityCode из первого города
                        return cities[0].CityCode;
                    }
                    else
                    {
                        // Иначе возвращаем -1
                        return -1;
                    }

                }
                else
                {
                    // Иначе возвращаем -1
                    return -1;
                }
            }
        }

        // Класс для хранения данных о городе от API СДЭК
        public class City
        {
            [JsonProperty("cityCode")]
            public int CityCode { get; set; }
        }

        static async Task Main(string[] args)
        {
            // Получаем cityCode для Санкт-Петербурга по ФИАС коду c2deb16a-0330-4f05-821f-1d09c93331e6
            var senderCityCode = await GetCityCodeByFias("c2deb16a-0330-4f05-821f-1d09c93331e6");
            Console.WriteLine($"Код города Санкт-Петербурга: {senderCityCode}");

            // Получаем cityCode для Москвы по ФИАС коду 0c5b2444-70a0-4932-980c-b4dc0d3f02b5
            var receiverCityCode = await GetCityCodeByFias("0c5b2444-70a0-4932-980c-b4dc0d3f02b5");
            Console.WriteLine($"Код города Москвы: {receiverCityCode}");

            // Создаем объект Request с нужными параметрами, используя полученные cityCode вместо ФИАС кодов городов
            var request = new Request()
            {
                Version = "1.0",
                DateExecute = DateTime.Now.ToString("yyyy-MM-dd"),
                SenderCityId = senderCityCode,
                /* SenderCityId = new Location()
                 {
                     FiasGuid = "c2deb16a-0330-4f05-821f-1d09c93331e6"
                 },*/
                ReceiverCityId = receiverCityCode,
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