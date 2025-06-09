# AwesomeStream

A collection of practical .NET Stream inheritors.

[![NuGet](https://img.shields.io/nuget/v/EggEgg.AwesomeStream.svg)](https://www.nuget.org/packages/EggEgg.AwesomeStream)

## Features

- `CombinedStream`: Implement a `Stream` that concatenates multiple `Stream`.
- `SpanAccessStream`: Implement a `Stream` that maps a specified range of provided `Stream`.
- `SpeedRecordReadStream`: A `Stream` read wrapper to record the number of bytes passed from read attempts.
- `MonitorStream`: Designed for monitoring a certain `Stream`'s availability (often in read-while-download situations, like video streaming).
- `Base64EncodeStream`: Implement a transform stream that make `input` be able to be read as ASCII character stream of its base64 encode result.
