import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@innovark-weather/components"
import type { WeatherHistoryResponse } from "@web/client/api/generated/model"

interface IWeatherHistoryTableProps {
  readonly history: WeatherHistoryResponse
}

// Times are ISO 8601 in UTC+7, e.g. 2026-09-28T14:00:00+07:00, shown as 2026-09-28 14:00.
function formatTime(isoTime: string) {
  return `${isoTime.slice(0, 10)} ${isoTime.slice(11, 16)}`
}

// The API's records, newest first: the requested hour and the 9 hours before it.
export function WeatherHistoryTable({ history }: IWeatherHistoryTableProps) {
  return (
    <Table>
      <TableCaption>
        {history.records.length} hourly records up to {formatTime(history.requestedTime)} (UTC+7),
        newest first.
      </TableCaption>
      <TableHeader>
        <TableRow>
          <TableHead>Time (UTC+7)</TableHead>
          <TableHead className="text-right">°C</TableHead>
          <TableHead className="text-right">°F</TableHead>
          <TableHead className="text-right">Humidity (%)</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {history.records.map((record) => (
          <TableRow key={record.time}>
            <TableCell>{formatTime(record.time)}</TableCell>
            <TableCell className="text-right tabular-nums">
              {record.temperatureC.toFixed(1)}
            </TableCell>
            <TableCell className="text-right tabular-nums">
              {record.temperatureF.toFixed(1)}
            </TableCell>
            <TableCell className="text-right tabular-nums">{record.relativeHumidity}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
