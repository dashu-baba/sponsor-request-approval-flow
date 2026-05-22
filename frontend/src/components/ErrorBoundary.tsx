import { Component, type ErrorInfo, type ReactNode } from 'react'

import { ErrorState } from '@/components/states/query-states'

interface ErrorBoundaryProps {
  children: ReactNode
  fallbackTitle?: string
}

interface ErrorBoundaryState {
  error: Error | null
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('UI error boundary caught:', error, info)
  }

  private handleRetry = (): void => {
    this.setState({ error: null })
  }

  render(): ReactNode {
    if (this.state.error) {
      return (
        <ErrorState
          title={this.props.fallbackTitle ?? 'This section failed to load'}
          message={this.state.error.message}
          onRetry={this.handleRetry}
        />
      )
    }

    return this.props.children
  }
}
