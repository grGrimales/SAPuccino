using CoffeeTracker.Api.Application.Models;

namespace CoffeeTracker.Api.Application.State;

/// <summary>
/// Mantém em memória o estado vivo da máquina (fase 1): conexão, cafés do dia
/// e última utilização. É um singleton thread-safe: o assinante MQTT o atualiza
/// e o endpoint de estado e o broadcast do SignalR o leem.
/// </summary>
public sealed class MachineStateTracker(TimeZoneInfo timeZone)
{
    private readonly object _gate = new();
    private bool _brokerConnected;
    private bool _inUse;
    private DateTime? _lastUsedUtc;
    private DateOnly _countedLocalDate;
    private int _coffeesToday;

    /// <summary>Marca se o backend está conectado ao broker MQTT.</summary>
    public void SetBrokerConnected(bool connected)
    {
        lock (_gate)
        {
            _brokerConnected = connected;
        }
    }

    /// <summary>Atualiza se a máquina está preparando um café. Retorna true se o estado mudou.</summary>
    public bool SetInUse(bool inUse)
    {
        lock (_gate)
        {
            if (_inUse == inUse)
            {
                return false;
            }

            _inUse = inUse;
            return true;
        }
    }

    /// <summary>Registra um café detectado: atualiza a última utilização e o contador do dia.</summary>
    public void RecordCoffee(DateTime occurredUtc)
    {
        lock (_gate)
        {
            _lastUsedUtc = occurredUtc;

            var localDate = LocalDateOf(occurredUtc);
            if (localDate != _countedLocalDate)
            {
                _countedLocalDate = localDate;
                _coffeesToday = 0;
            }

            _coffeesToday++;
        }
    }

    /// <summary>Hidrata o contador do dia a partir de dados persistidos na inicialização (melhor esforço).</summary>
    public void Seed(DateOnly localDate, int coffeesToday, DateTime? lastUsedUtc)
    {
        lock (_gate)
        {
            _countedLocalDate = localDate;
            _coffeesToday = coffeesToday;
            _lastUsedUtc = lastUsedUtc;
        }
    }

    /// <summary>Retorna o estado atual; reinicia o contador se o dia local já mudou.</summary>
    public CoffeeStatus GetSnapshot(DateTime nowUtc)
    {
        lock (_gate)
        {
            var today = LocalDateOf(nowUtc);
            var coffees = today == _countedLocalDate ? _coffeesToday : 0;

            return new CoffeeStatus
            {
                MachineState = _brokerConnected ? "online" : "offline",
                InUse = _inUse,
                CoffeesToday = coffees,
                LastUsedUtc = _lastUsedUtc
            };
        }
    }

    /// <summary>
    /// Calcula a janela UTC [início, fim) do dia local atual e a data local correspondente.
    /// Usado para hidratar o contador de "cafés de hoje" a partir do banco na inicialização.
    /// </summary>
    public (DateTime FromUtc, DateTime ToUtc, DateOnly LocalDate) GetTodayWindow(DateTime nowUtc)
    {
        var localDate = LocalDateOf(nowUtc);
        var startLocal = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone);
        return (fromUtc, fromUtc.AddDays(1), localDate);
    }

    private DateOnly LocalDateOf(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            timeZone);
        return DateOnly.FromDateTime(local);
    }
}
