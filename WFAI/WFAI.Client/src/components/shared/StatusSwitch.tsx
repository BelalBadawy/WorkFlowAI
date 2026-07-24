import { Switch } from "../ui/switch"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "../ui/tooltip"

interface StatusSwitchProps {
  readonly isActive: boolean;
  readonly onToggle: () => void;
  readonly entityName: string;
  readonly isLoading?: boolean;
  readonly disabled?: boolean;
}

export function StatusSwitch({
  isActive,
  onToggle,
  entityName,
  isLoading = false,
  disabled = false,
}: StatusSwitchProps) {
  const tooltipText = isActive ? "Click to deactivate" : "Click to activate";

  return (
    <TooltipProvider>
      <Tooltip delayDuration={300}>
        <TooltipTrigger asChild>
          <span className="inline-flex">
            <Switch
              checked={isActive}
              onCheckedChange={onToggle}
              disabled={isLoading || disabled}
              aria-label={`${tooltipText} ${entityName}`}
            />
          </span>
        </TooltipTrigger>
        <TooltipContent side="top">
          <p>{tooltipText}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}