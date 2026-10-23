import { InputAdornment, TextField } from '@mui/material';
import { ChangeEvent, FC, forwardRef, useState } from 'react';
import { NumericFormat, NumericFormatProps } from 'react-number-format';
import { Controller } from 'react-hook-form';
//----------------------------------------------------------------
interface CustomProps {
  onChange: (event: { target: { name: string; value: string } }) => void;
  name: string;
}
interface IlgNumbericFormat {
  name: string;
  control: any;
  label: string;
}
const NumericFormatCustom = forwardRef<NumericFormatProps, CustomProps>(
  function NumericFormatCustom(props, ref) {
    const { onChange, ...other } = props;

    return (
      <NumericFormat
        {...other}
        getInputRef={ref}
        onValueChange={(values) => {
          onChange({
            target: {
              name: props.name,
              value: values.value,
            },
          });
        }}
        thousandSeparator
        valueIsNumericString
      />
    );
  }
);
const LgNumbericFormat: FC<IlgNumbericFormat> = ({ name, label, control, ...others }) => {
  // const [values, setValues] = useState('');
  //
  // const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
  //     setValues(event.target.value);
  // };
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState: { error } }) => (
        <TextField
          {...field}
          value={field.value}
          onChange={(e) => field.onChange(e.target.value)}
          id="formatted-numberformat-input"
          slotProps={{
            input: {
              inputComponent: NumericFormatCustom as any,
              endAdornment: <InputAdornment position="end">ریال</InputAdornment>,
            },
          }}
          error={!!error}
          helperText={error ? error.message : ''}
          label={label}
          {...others}
        />
      )}
    />
  );
};
export default LgNumbericFormat;
