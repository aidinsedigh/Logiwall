import { Controller, useFormContext } from 'react-hook-form';
import { Box, TextField } from '@mui/material';
import { FC, forwardRef, useState } from 'react';
import PhoneNumberInput from 'react-phone-number-input/input';

//----------------------------------------------------------
interface IphoneNumber {
  name: string;
  helperText?: string;
  fullWidth?: boolean;
}
//----------------------------------------------------------

const PhoneNumberField: FC<IphoneNumber> = ({ name, fullWidth, helperText, ...other }) => {
  const { control, setValue } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState: { error } }) => (
        <Box sx={{ textAlign: 'center' }}>
          <Box sx={{ display: 'inline-block', width: fullWidth ? '100%' : '330px' }}>
            <PhoneNumberInput
              inputComponent={CustomInput}
              value={field.value}
              onChange={(newValue: any) => {
                setValue(name, newValue, { shouldValidate: true });
              }}
              country="IR"
              error={!!error}
              helperText={error ? error?.message : helperText}
              {...other}
            />
          </Box>
        </Box>
      )}
    />
  );
};

export default PhoneNumberField;

const CustomInput: any = forwardRef(({ ...props }: any, ref) => (
  <TextField fullWidth inputRef={ref} {...props} label="شماره تلفن همراه" />
));
