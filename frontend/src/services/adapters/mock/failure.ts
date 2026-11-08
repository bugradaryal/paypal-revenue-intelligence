let failing = false

/** Lets the data inspector page exercise error states. Mock only. */
export function setMockFailure(value: boolean) {
  failing = value
}
export function isMockFailing() {
  return failing
}
