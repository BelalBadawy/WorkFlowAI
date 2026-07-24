import { format } from "date-fns"
import { Calendar as CalendarIcon } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "./button"
import { Calendar } from "./calendar"
import { Popover, PopoverContent, PopoverTrigger } from "./popover"

export interface DatePickerProps {
  readonly date?: Date
  readonly setDate: (date?: Date) => void
  readonly placeholder?: string
  readonly className?: string
}

export function DatePicker({
  date,
  setDate,
  placeholder = "Pick a date",
  className,
}: DatePickerProps) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          className={cn(
            "w-full justify-start text-left font-normal h-8 text-xs px-3 rounded-xl border border-neutral-300 bg-neutral-50/50 hover:bg-neutral-100 whitespace-nowrap outline-none transition-all flex items-center gap-2",
            !date && "text-neutral-500",
            className
          )}
        >
          <CalendarIcon className="h-3.5 w-3.5 text-neutral-500" />
          {date ? format(date, "yyyy/MM/dd") : <span>{placeholder}</span>}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={date}
          onSelect={setDate}
        />
      </PopoverContent>
    </Popover>
  )
}