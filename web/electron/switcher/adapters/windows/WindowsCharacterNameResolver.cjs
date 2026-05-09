class WindowsCharacterNameResolver {
  async resolve(_pid) {
    throw new Error('WindowsCharacterNameResolver not implemented')
  }
}
module.exports = { WindowsCharacterNameResolver }