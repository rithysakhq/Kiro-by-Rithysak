const copyButton = document.querySelector("[data-copy-checksum]");
const checksum = document.querySelector("#checksum");

if (copyButton && checksum) {
  copyButton.addEventListener("click", async () => {
    const value = checksum.textContent.trim();

    try {
      await navigator.clipboard.writeText(value);
      copyButton.textContent = "Copied";
      window.setTimeout(() => {
        copyButton.textContent = "Copy";
      }, 1400);
    } catch {
      copyButton.textContent = "Select";
      checksum.focus();
    }
  });
}
