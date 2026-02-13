using System;
using System.Text.Json;

namespace CCCamScraper.Models;

/// <summary>
/// Represents the status of an OSCam reader with error counts and user information.
/// </summary>
public class OsCamReaderDescription
{
    /// <summary>
    /// Parameterless constructor for JSON deserialization or manual instantiation.
    /// </summary>
    public OsCamReaderDescription()
    {
        Username = string.Empty;
    }

    /// <summary>
    /// Gets the number of error occurrences.
    /// </summary>
    public uint AccumulatedError { get; init; }

    /// <summary>
    /// Gets the number of times the reader was off.
    /// </summary>
    public uint AccumulatedOff { get; init; }

    /// <summary>
    /// Gets the number of unknown states.
    /// </summary>
    public uint AccumulatedUnknown { get; init; }

    /// <summary>
    /// Gets the load balancer value for the reader.
    /// </summary>
    public uint LbValueReader { get; init; }

    /// <summary>
    /// Gets or sets the username associated with the reader.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets the number of successful ECM counts.
    /// </summary>
    public uint ECMOK { get; init; }

    /// <summary>
    /// Gets the number of failed ECM counts.
    /// </summary>
    public uint ECMNOK { get; init; }

    /// <summary>
    /// Gets the number of ECM timeout counts.
    /// </summary>
    public uint ECMTOUT { get; init; }

    /// <summary>
    /// Updates the reader status based on the new state.
    /// </summary>
    /// <param name="newFoundState">The new state (off, unknown, error, lbvaluereader, ecmok, ecmnok, ecmtout, or other).</param>
    /// <returns>A new instance with the updated state.</returns>
    public OsCamReaderDescription UpdateDescriptionWithNewData(string newFoundState)
    {
        return newFoundState.ToLower() switch
        {
            "off" => new OsCamReaderDescription
            {
                AccumulatedError = AccumulatedError,
                AccumulatedOff = AccumulatedOff + 1,
                AccumulatedUnknown = AccumulatedUnknown,
                LbValueReader = LbValueReader,
                Username = Username,
                ECMOK = ECMOK,
                ECMNOK = ECMNOK,
                ECMTOUT = ECMTOUT
            },
            "unknown" => new OsCamReaderDescription
            {
                AccumulatedError = AccumulatedError,
                AccumulatedOff = AccumulatedOff,
                AccumulatedUnknown = AccumulatedUnknown + 1,
                LbValueReader = LbValueReader,
                Username = Username,
                ECMOK = ECMOK,
                ECMNOK = ECMNOK,
                ECMTOUT = ECMTOUT
            },
            "error" => new OsCamReaderDescription
            {
                AccumulatedError = AccumulatedError + 1,
                AccumulatedOff = AccumulatedOff,
                AccumulatedUnknown = AccumulatedUnknown,
                LbValueReader = LbValueReader,
                Username = Username,
                ECMOK = ECMOK,
                ECMNOK = ECMNOK,
                ECMTOUT = ECMTOUT
            },
            "lbvaluereader" => new OsCamReaderDescription
            {
                AccumulatedError = AccumulatedError,
                AccumulatedOff = AccumulatedOff,
                AccumulatedUnknown = AccumulatedUnknown,
                LbValueReader = LbValueReader + 1,
                Username = Username,
                ECMOK = ECMOK,
                ECMNOK = ECMNOK,
                ECMTOUT = ECMTOUT
            },
            "ecmok" => new OsCamReaderDescription
            {
                AccumulatedError = AccumulatedError,
                AccumulatedOff = AccumulatedOff,
                AccumulatedUnknown = AccumulatedUnknown,
                LbValueReader = LbValueReader,
                Username = Username,
                ECMOK = ECMOK + 1,
                ECMNOK = ECMNOK,
                ECMTOUT = ECMTOUT
            },
            "ecmnok" => new OsCamReaderDescription
            {
                AccumulatedError = AccumulatedError,
                AccumulatedOff = AccumulatedOff,
                AccumulatedUnknown = AccumulatedUnknown,
                LbValueReader = LbValueReader,
                Username = Username,
                ECMOK = ECMOK,
                ECMNOK = ECMNOK + 1,
                ECMTOUT = ECMTOUT
            },
            "ecmtout" => new OsCamReaderDescription
            {
                AccumulatedError = AccumulatedError,
                AccumulatedOff = AccumulatedOff,
                AccumulatedUnknown = AccumulatedUnknown,
                LbValueReader = LbValueReader,
                Username = Username,
                ECMOK = ECMOK,
                ECMNOK = ECMNOK,
                ECMTOUT = ECMTOUT + 1
            },
            _ => new OsCamReaderDescription
            {
                AccumulatedError = 0,
                AccumulatedOff = 0,
                AccumulatedUnknown = 0,
                LbValueReader = 0,
                Username = Username,
                ECMOK = 0,
                ECMNOK = 0,
                ECMTOUT = 0
            }
        };
    }

    /// <summary>
    /// Returns a string representation of the reader status.
    /// </summary>
    /// <returns>A semicolon-separated string of the format: Error;Off;Unknown;LbValueReader;Username;ECMOK;ECMNOK;ECMTOUT</returns>
    public override string ToString()
    {
        return string.Join(';', AccumulatedError, AccumulatedOff, AccumulatedUnknown, LbValueReader, Username, ECMOK, ECMNOK, ECMTOUT);
    }

    /// <summary>
    /// Serializes the instance to a JSON string.
    /// </summary>
    /// <returns>A JSON string representing the instance.</returns>
    public string Serialize()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an OsCamReaderDescription instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A new OsCamReaderDescription instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if deserialization fails.</exception>
    public static OsCamReaderDescription Deserialize(string json)
    {
        return JsonSerializer.Deserialize<OsCamReaderDescription>(json)
            ?? throw new InvalidOperationException("Failed to deserialize JSON to OsCamReaderDescription");
    }
}