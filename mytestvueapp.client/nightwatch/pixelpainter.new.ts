import {assert, ExtendDescribeThis} from 'nightwatch';

interface CustomThis {
  pixelpainterurl_base: string;
  pixelpainterurl_goto: string;
}

// callback passed to `describe` should be a regular function (not an arrow function).
describe('Pixel Painter Tests', function(this: ExtendDescribeThis<CustomThis>) {
  this.pixelpainterurl_base = 'https://localhost:5173/';
  this.pixelpainterurl_goto = this.pixelpainterurl_base + "new"
  
  // callback can be a regular function as well as an arrow function.
  beforeEach(function(this: ExtendDescribeThis<CustomThis>, browser) {
    browser.navigateTo(this.pixelpainterurl_goto!);
  });

  it('Color Picker', (browser) => {
    browser.element.find('.p-colorpicker-preview').assert.present()
  });

  it('Canvas Resolution', (browser) => {
    browser.element.find('.p-inputnumber-input').assert.hasClass("p-inputnumber-input")
    
  });

  it('Home button', (browser) => {
    browser
      .element.findByText('Home')
      .assert.visible();
    browser
      .element.findByText('Home')
      .click();
    browser.assert.urlEquals(this.pixelpainterurl_base);
  });

  it('Painter button', (browser) => {
    browser
      .element.findByText('Painter')
      .assert.visible();
    browser
      .element.findByText('Painter')
      .click();
    browser.assert.urlEquals(this.pixelpainterurl_base + "new");
  });

  it('Gallery button', (browser) => {
    browser
      .element.findByText('Gallery')
      .assert.visible();
    browser
      .element.findByText('Gallery')
      .click();
    browser.assert.urlEquals(this.pixelpainterurl_base + "gallery");
  });
});
