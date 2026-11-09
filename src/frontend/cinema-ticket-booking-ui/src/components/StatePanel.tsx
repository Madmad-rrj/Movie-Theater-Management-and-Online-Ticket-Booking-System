interface StatePanelProps {
  title: string;
  message: string;
  actionLabel?: string;
  onAction?: () => void;
}

export function StatePanel({ title, message, actionLabel, onAction }: StatePanelProps) {
  return (
    <div className="state-panel" role="status">
      <span className="state-panel__mark">✦</span>
      <h2>{title}</h2>
      <p>{message}</p>
      {actionLabel && onAction && (
        <button className="button button--outline" type="button" onClick={onAction}>
          {actionLabel}
        </button>
      )}
    </div>
  );
}
