export function shouldRenderNeutralShell(
  hydrated: boolean,
  status: "checking" | "authenticated" | "guest",
  protectedPage: boolean,
) {
  return !hydrated || status === "checking" || (status === "guest" && protectedPage);
}
