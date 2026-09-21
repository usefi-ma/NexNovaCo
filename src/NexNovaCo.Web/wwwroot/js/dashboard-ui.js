// Presentation bridge only: MudMenuItem is not a native submit button.
// Cookies and antiforgery remain owned by the existing server-side POST endpoint.
export function submitLogout(form) {
    form.requestSubmit();
}
