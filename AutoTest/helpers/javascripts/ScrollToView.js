arguments[0].scrollIntoView();
window.scrollBy(0, -(window.scrollY - document.getOffsetRect(arguments[0]).top + arguments[1]));