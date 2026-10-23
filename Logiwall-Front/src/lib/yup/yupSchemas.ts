import * as yup from 'yup';

//------------------------------------------------------------------
export const NumberFieldSchema = (
  name: string,
  min: number,
  max: number,
  minMessage: string,
  maxMessage: string
) =>
  yup
    .string()
    .matches(/^\d+$/, `${name} باید عددی باشد `) // Ensures input contains only digits
    .required(`${name} را وارد کنید `) // Makes the field required
    .test('min-value', minMessage, (value) => {
      if (value) return Number(value) >= min;
      return true;
    })
    .test('max-value', maxMessage, (value) => {
      if (value) return Number(value) <= max;
      return true;
    });
