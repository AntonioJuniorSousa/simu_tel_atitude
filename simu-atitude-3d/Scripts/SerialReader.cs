using Godot;
using System;
using System.IO.Ports;
using System.Threading;
using System.Globalization;

public partial class SerialReader : Node
{
    [Export] public string PortName = "COM5";
    [Export] public int BaudRate = 115200;

    private SerialPort _serialPort;
    private Thread _readThread;
    private bool _keepReading = false;

    // Variáveis thread-safe
    private readonly object _lock = new object();
    private Quaternion _latestQuaternion = Quaternion.Identity;
    private int _packetCount = 0;
    private int _errorCount = 0;
    private DateTime _lastPacketTime = DateTime.Now;
    private int _safePacketCounter = 0;

    // Métricas acessíveis de fora
    public Quaternion LatestQuaternion { get; private set; } = Quaternion.Identity;
    public int CurrentHz { get; private set; } = 0;
    public int ErrorCount { get; private set; } = 0;
    public DateTime LastPacketTime { get; private set; } = DateTime.Now;
    public float CurrentG { get; private set; } = 1.0f;

    private double _timeAccumulator = 0;

    public override void _Ready()
    {
        try
        {
            _serialPort = new SerialPort(PortName, BaudRate);
            _serialPort.ReadTimeout = 500;
            _serialPort.Open();
            _keepReading = true;
            _readThread = new Thread(ReadPort);
            _readThread.Start();
            GD.Print($"Serial via {PortName} iniciada.");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Falha ao abrir porta serial: {e.Message}");
        }
    }

    public override void _Process(double delta)
    {
        lock (_lock)
        {
            // Atualiza propriedades para acesso das outras classes
            LatestQuaternion = _latestQuaternion;
            ErrorCount = _errorCount;
            LastPacketTime = _lastPacketTime;
        }

        // Calcula packets per second (Hz)
        _timeAccumulator += delta;
        if (_timeAccumulator >= 1.0)
        {
            lock (_lock)
            {
                CurrentHz = _safePacketCounter;
                _safePacketCounter = 0;
            }
            _timeAccumulator = 0.0;
        }
    }

    private void ReadPort()
    {
        while (_keepReading && _serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                string line = _serialPort.ReadLine().Trim();
                ParseLine(line);
                lock (_lock) { _safePacketCounter++; }
            }
            catch (TimeoutException) { }
            catch (Exception e)
            {
                GD.PrintErr($"Erro lendo serial: {e.Message}");
                lock (_lock) { _errorCount++; }
            }
        }
    }

    // Nota para firmware (ESP32):
    // Serial.print(q0,4); Serial.print(","); Serial.print(q1,4); Serial.print(",");
    // Serial.print(q2,4); Serial.print(","); Serial.print(q3,4); Serial.print(",");
    // Serial.print(ax,3); Serial.print(","); Serial.print(ay,3); Serial.print(",");
    // Serial.println(az,3);
    private void ParseLine(string line)
    {
        string[] parts = line.Split(',');
        if (parts.Length == 4 || parts.Length == 7)
        {
            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float w) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                lock (_lock)
                {
                    _latestQuaternion = new Quaternion(x, y, z, w);
                    _packetCount++;
                    _lastPacketTime = DateTime.Now;
                }
                
                if (parts.Length == 7 &&
                    float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float ax) &&
                    float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float ay) &&
                    float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float az))
                {
                    CurrentG = Mathf.Sqrt(ax * ax + ay * ay + az * az);
                }
                else
                {
                    CurrentG = 1.0f;
                }
            }
            else
            {
                lock (_lock) { _errorCount++; }
            }
        }
    }

    public override void _ExitTree()
    {
        _keepReading = false;
        if (_readThread != null && _readThread.IsAlive)
            _readThread.Join(500);

        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.DiscardInBuffer();
            _serialPort.Close();
        }
    }
}
