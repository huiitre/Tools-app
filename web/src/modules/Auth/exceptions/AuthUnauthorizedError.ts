export class AuthUnauthorizedError extends Error {
  constructor(message: string = 'Unauthorized') {
    super(message)
    this.name = 'AuthUnauthorizedError'
  }
}