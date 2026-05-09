export interface ICharacterNameResolver {
  resolve(pid: number): Promise<string | null>
}