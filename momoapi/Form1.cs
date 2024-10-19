using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace momoapi
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();

        public Form1()
        {
            InitializeComponent();
            listView1.Columns.Add("Số điện thoại", 100);
            listView1.Columns.Add("Tên", 400);
            listView1.Columns.Add("Tổng tiền", 200);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string inputPhones = textBox1.Text; // Get all phone numbers from input
            string[] phoneNumbers = inputPhones.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Split by comma

            foreach (var phoneNumber in phoneNumbers)
            {
                string trimmedPhone = phoneNumber.Trim(); // Trim whitespace
                if (!string.IsNullOrEmpty(trimmedPhone))
                {
                    await LoginAndRetrieveData(trimmedPhone);
                }
            }
        }

        private async Task LoginAndRetrieveData(string phoneNumber)
        {
            try
            {
                // Step 1: Login and get the token
                string loginUrl = "https://business.momo.vn/api/authentication/login?language=vi";
                string loginPayload = JsonConvert.SerializeObject(new
                {
                    username = phoneNumber, // Use the current phone number
                    password = "vanhuY90$"  // Default password
                });

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    HttpContent content = new StringContent(loginPayload, Encoding.UTF8, "application/json");
                    HttpResponseMessage loginResponse = await client.PostAsync(loginUrl, content);

                    if (loginResponse.IsSuccessStatusCode)
                    {
                        // Step 2: Parse the response to get the token
                        var loginResponseData = await loginResponse.Content.ReadAsStringAsync();
                        dynamic loginResult = JsonConvert.DeserializeObject(loginResponseData);

                        string token = loginResult?.data.token; // Adjust field name based on actual API response

                        if (!string.IsNullOrEmpty(token))
                        {
                            string merchantUrl = "https://business.momo.vn/api/profile/v2/merchants?requestType=LOGIN_MERCHANTS&language=vi";
                            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                            HttpResponseMessage merchantResponse = await client.GetAsync(merchantUrl);

                            if (merchantResponse.IsSuccessStatusCode)
                            {
                                var merchantData = await merchantResponse.Content.ReadAsStringAsync();
                                dynamic merchantResult = JsonConvert.DeserializeObject(merchantData);

                                // Extract merchantId from response
                                string merchantId = merchantResult?.data.merchantResponseList[0].id; // Adjust based on actual response structure
                                string merchantName = merchantResult?.data.merchantResponseList[0].brandName;

                                if (!string.IsNullOrEmpty(merchantId))
                                {
                                    DateTime today = DateTime.Today; // Get today's date
                                    DateTime startOfMonth = new DateTime(today.Year, today.Month, 1); // First day of the current month
                                    DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                                    string fromDate = startOfMonth.ToString("yyyy-MM-dd"); // Beginning of the day
                                    string toDate = endOfMonth.ToString("yyyy-MM-dd"); // End of the day
                                 
                                    // Step 4: Fetch transaction data
                                   

                                    string transactionsUrl = $"https://business.momo.vn/api/transaction/v2/transactions/statistics?pageSize=10&pageNumber=0&fromDate={fromDate}T00%3A00%3A00.00&toDate={toDate}T23%3A59%3A59.00&status=ALL&merchantId={merchantId}&language=vi";

                                    HttpResponseMessage transactionResponse = await client.GetAsync(transactionsUrl);

                                    if (transactionResponse.IsSuccessStatusCode)
                                    {
                                        var transactionData = await transactionResponse.Content.ReadAsStringAsync();
                                        dynamic transactions = JsonConvert.DeserializeObject(transactionData);
                                        string total = transactions.data.totalSuccessAmount;

                                        // Add data to the ListView
                                        ListViewItem item = new ListViewItem(phoneNumber);
                                        item.SubItems.Add(merchantName);
                                        item.SubItems.Add(total + "đ");
                                        listView1.Items.Insert(0, item);
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Failed to retrieve transactions for {phoneNumber}.");
                                    }
                                }
                                else
                                {
                                    MessageBox.Show($"Merchant ID not found for {phoneNumber}.");
                                }
                            }
                            else
                            {
                                MessageBox.Show($"Failed to retrieve merchant information for {phoneNumber}.");
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Login successful but token not found in response for {phoneNumber}.");
                        }
                    }
                    else
                    {
                        string loginErrorResponse = await loginResponse.Content.ReadAsStringAsync();
                        MessageBox.Show($"Login failed for {phoneNumber}. Status code: {loginResponse.StatusCode}, Response: {loginErrorResponse}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error for " + phoneNumber + ": " + ex.Message);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // Logic for handling text input (if needed)
        }
        
    }
}
