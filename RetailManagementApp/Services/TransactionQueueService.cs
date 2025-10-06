using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using RetailManagementApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RetailManagementApp.Services
{
    public class TransactionQueueService
    {
        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        private readonly QueueClient _queueClient;

        public TransactionQueueService(string storageConnectionString, string queueName)
        {
            var queueServiceClient = new QueueServiceClient(storageConnectionString);
            _queueClient = queueServiceClient.GetQueueClient(queueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task SendTransactionMessageAsync(object message)
        {
            var messageJson = JsonSerializer.Serialize(message);

            await _queueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson)));
        }

        public async Task<List<QueueLogViewModel>> PeekMessagesAsync()
        {
            var messageList = new List<QueueLogViewModel>();
            var messages = await _queueClient.PeekMessagesAsync(maxMessages: 32);

            foreach (PeekedMessage message in messages.Value)
            {

                messageList.Add(new QueueLogViewModel
                {
                    Id = message.MessageId,
                    QueueTime = message.InsertedOn,
                    MessageText = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.Body.ToString()))
                });
            }

            return messageList;
        }

        public async Task<T?> ProcessNextMessageAsync<T>() where T : class
        {
            var response = await _queueClient.ReceiveMessagesAsync(maxMessages: 1);
            var messages = response.Value;

            if (messages != null && messages.Any())
            {
                var message = messages.First();
                try
                {
                    var messageText = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
                    var deserializedObject = JsonSerializer.Deserialize<T>(messageText);

                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                    return deserializedObject;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing queue message: {ex.Message}");
                    return null;
                }
            }

            return null;
        }
    }
}

