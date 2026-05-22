export function AppFooter() {
  return (
    <footer className="ml-[var(--sidebar-width)] border-t border-border bg-surface px-7 py-4 text-[12px] text-text-hint">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span>© {new Date().getFullYear()} SponTrack. Internal use only.</span>
        <div className="flex items-center gap-4">
          <span className="cursor-not-allowed opacity-60">Help centre</span>
          <span className="cursor-not-allowed opacity-60">Contact support</span>
        </div>
      </div>
    </footer>
  )
}
