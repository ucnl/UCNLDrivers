// UCNLDrivers/uAux/uAuxUDPDataSource.cs
using System.Net;
using System.Net.Sockets;
using System.Text;
using UCNLNMEA;

namespace UCNLDrivers.uAux
{
    public class uAuxUDPDataSource : uAuxDataSource, IAuxSource
    {
        private UdpClient? _client;
        private CancellationTokenSource? _cts;
        private readonly int _listenPort;
        private IPEndPoint? _remoteEndPoint;

        public int ListenPort => _listenPort;
        public IPEndPoint? RemoteEndPoint => _remoteEndPoint;
        public bool HasRemote => _remoteEndPoint != null;

        #region IAuxSource

        public string Id { get; }
        public AuxSourceKind Kind { get; }

        private AuxStatus _status = AuxStatus.Inactive;
        public AuxStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        string? IAuxSource.PortName => null;

        public event EventHandler? OnStatusChanged;

        #endregion

        public uAuxUDPDataSource(string id, AuxSourceKind kind, int listenPort, string? description = null)
        {
            Id = id;
            Kind = kind;
            _listenPort = listenPort;
            Description = description ?? $"UDP :{listenPort}";
        }

        public void SetRemote(IPEndPoint remote)
        {
            _remoteEndPoint = remote;
        }

        public void SetRemote(string host, int port)
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
        }

        public override void Start()
        {
            if (IsActive) return;

            _cts = new CancellationTokenSource();

            try
            {
                _client = new UdpClient(_listenPort);
                IsActive = true;
                Status = AuxStatus.Detected;

                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                    $"{Description}: Listening on port {_listenPort}"));

                _ = ReceiveLoop(_cts.Token);
            }
            catch (Exception ex)
            {
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR,
                    $"{Description}: Start error: {ex.Message}"));
                IsActive = false;
                Status = AuxStatus.Error;
            }
        }

        public override void Stop()
        {
            _cts?.Cancel();
            try { _client?.Close(); } catch { }
            _client?.Dispose();
            _client = null;
            IsActive = false;
            Status = AuxStatus.Inactive;

            OnClosed();
            LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                $"{Description}: Stopped"));
        }

        public override void Send(string message)
        {
            if (_client == null || !IsActive || _remoteEndPoint == null) return;
            SendRaw(Encoding.ASCII.GetBytes(message));
        }

        public override void SendRaw(byte[] data)
        {
            if (_client == null || !IsActive || _remoteEndPoint == null) return;
            _client.Send(data, data.Length, _remoteEndPoint);
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _client != null)
            {
                try
                {
                    var result = await _client.ReceiveAsync();
                    if (ct.IsCancellationRequested) break;

                    var message = Encoding.ASCII.GetString(result.Buffer);

                    if (IsLogIncoming)
                        LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                            $"{Description} >> {message}"));

                    RawDataReceived?.Invoke(this, new RawDataReceivedEventArgs(result.Buffer));

                    try
                    {
                        var sentence = NMEAParser.Parse(message);
                        ProcessIncoming(sentence);
                    }
                    catch (Exception ex)
                    {
                        LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR,
                            $"{Description}: Parse error: {ex.Message}"));
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR,
                        $"{Description}: Receive error: {ex.Message}"));
                    if (!ct.IsCancellationRequested)
                        await Task.Delay(1000, ct);
                }
            }
        }

        public override void ProcessIncoming(NMEASentence sentence)
        {
            // Переопределяется в наследниках
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                _cts?.Dispose();
            }
        }
    }
}