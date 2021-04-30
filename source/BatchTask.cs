//
// Copyright (c) 2021 karamem0
//
// This software is released under the MIT License.
//
// https://github.com/karamem0/inkbird/blob/master/LICENSE
//

using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Karamem0.Inkbird
{

    public class BatchTask
    {

        private readonly ILogger logger;

        private readonly CommandLineOptions options;

        private readonly CancellationTokenSource cancellationTokenSource;

        public BatchTask(ILogger<BatchTask> logger, CommandLineOptions options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

        public Task ExecuteAsync()
        {
            return Task.Run(() =>
            {
                var watcher = new BluetoothLEAdvertisementWatcher();
                watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                watcher.Received += async (sender, e) =>
                {
                    try
                    {
                        var macAddress = string.Join(":",
                        BitConverter.GetBytes(e.BluetoothAddress)
                            .Reverse()
                            .Select(x => x.ToString("X2")))
                        .Substring(6);
                        if (string.Equals(macAddress, this.options.MacAddress, StringComparison.OrdinalIgnoreCase) != true)
                        {
                            return;
                        }
                        if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
                        {
                            watcher.Stop();
                        }
                        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);
                        if (device == null)
                        {
                            throw new ApplicationException("Cannot find BLE device");
                        }
                        this.logger.LogTrace($"BLE device: {macAddress}");
                        var uuid = e.Advertisement.ServiceUuids.FirstOrDefault();
                        var services = await device.GetGattServicesForUuidAsync(uuid);
                        if (services.Status != GattCommunicationStatus.Success)
                        {
                            throw new ApplicationException($"Cannot find GATT service: {services.Status}");
                        }
                        var service = services.Services.FirstOrDefault();
                        if (service == null)
                        {
                            throw new ApplicationException("Cannot find GATT service");
                        }
                        this.logger.LogTrace($"GATT service: {service.Uuid}");
                        var characteristics = await service.GetCharacteristicsAsync();
                        if (characteristics.Status != GattCommunicationStatus.Success)
                        {
                            throw new ApplicationException($"Cannot find GATT characteristic: {characteristics.Status}");
                        }
                        var characteristic = characteristics.Characteristics
                                .Where(x => x.AttributeHandle == Convert.ToUInt16(this.options.AttributeHandle, 16))
                                .FirstOrDefault();
                        if (characteristic == null)
                        {
                            throw new ApplicationException("Cannot find GATT characteristic");
                        }
                        this.logger.LogTrace($"GATT characteristic: {characteristic.Uuid}");
                        var value = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                        if (value.Status != GattCommunicationStatus.Success)
                        {
                            throw new ApplicationException("Cannot read value");
                        }
                        var buffer = value.Value.ToArray();
                        this.logger.LogTrace($"Read value: {BitConverter.ToString(buffer).Replace("-", " ")}");
                        using (var client = DeviceClient.CreateFromConnectionString(this.options.AzureIoTHub))
                        {
                            var temperature = (double)BitConverter.ToInt16(buffer, 0) / 100;
                            var humidity = (double)BitConverter.ToInt16(buffer, 2) / 100;
                            var payload = JsonSerializer.Serialize(
                                new
                                {
                                    Location = options.Location,
                                    Temperature = temperature,
                                    Humidity = humidity
                                });
                            var message = new Message(Encoding.UTF8.GetBytes(payload));
                            await client.SendEventAsync(message);
                        }
                        this.cancellationTokenSource.Cancel();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Unhandled error has occurred");
                        this.cancellationTokenSource.Cancel();
                    }
                };
                watcher.Start();
            });
        }

    }

}
